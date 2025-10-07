(function () {
    function getCsrfToken() {
        const meta = document.querySelector('meta[name="csrf-token"]');
        return meta ? meta.getAttribute('content') || '' : '';
    }

    const STEP_TYPE_NORMAL = 'Normal';
    const STEP_TYPE_DECISION = 'KararNoktasi';

    function normalizeStepType(value) {
        if (typeof value === 'number') {
            return value === 1 ? STEP_TYPE_DECISION : STEP_TYPE_NORMAL;
        }

        if (typeof value === 'string') {
            const normalized = value.trim();
            if (!normalized) {
                return STEP_TYPE_NORMAL;
            }

            if (normalized.toLowerCase() === 'kararnoktasi' || normalized.toLowerCase() === 'decision') {
                return STEP_TYPE_DECISION;
            }

            if (normalized.toLowerCase() === 'normal') {
                return STEP_TYPE_NORMAL;
            }

            if (normalized === STEP_TYPE_DECISION || normalized === STEP_TYPE_NORMAL) {
                return normalized;
            }
        }

        return STEP_TYPE_NORMAL;
    }

    function parseInitialWorkflows(json) {
        if (!json) {
            return [];
        }

        try {
            const parsed = JSON.parse(json);
            if (!Array.isArray(parsed)) {
                return [];
            }

            return parsed.map((workflow) => ({
                id: typeof workflow?.id === 'number' ? workflow.id : null,
                title: typeof workflow?.title === 'string' ? workflow.title : 'Yeni İş Akışı',
                steps: Array.isArray(workflow?.steps)
                    ? workflow.steps.map((step) => {
                        const stepType = normalizeStepType(step?.stepType);
                        const baseNext = typeof step?.nextStepCode === 'string' ? step.nextStepCode : '';
                        let nextYes = typeof step?.nextStepYesCode === 'string' ? step.nextStepYesCode : '';
                        let nextNo = typeof step?.nextStepNoCode === 'string' ? step.nextStepNoCode : '';
                        let nextSingle = baseNext;

                        if (stepType === STEP_TYPE_DECISION) {
                            nextYes = nextYes || baseNext;
                            nextSingle = '';
                        }
                        else {
                            nextSingle = baseNext || nextYes;
                            nextYes = '';
                            nextNo = '';
                        }

                        return {
                            id: typeof step?.id === 'number' ? step.id : null,
                            sequenceCode: typeof step?.sequenceCode === 'string' ? step.sequenceCode : '',
                            description: typeof step?.description === 'string' ? step.description : '',
                            stepType,
                            role: typeof step?.role === 'string' ? step.role : '',
                            nextStepCode: nextSingle,
                            nextStepYesCode: nextYes,
                            nextStepNoCode: nextNo
                        };
                    })
                    : []
            }));
        }
        catch (error) {
            console.warn('İş akışı başlangıç verisi çözümlenemedi:', error);
            return [];
        }
    }

    function normalizeString(value, maxLength) {
        if (typeof value !== 'string') {
            return '';
        }

        const trimmed = value.trim();
        if (!trimmed) {
            return '';
        }

        if (trimmed.length <= maxLength) {
            return trimmed;
        }

        return trimmed.substring(0, maxLength);
    }

    function normalizeStepCode(value) {
        const normalized = normalizeString(value, 50);
        return normalized ? normalized.toUpperCase() : '';
    }

    function getSequencePrefixForIndex(index) {
        if (typeof index !== 'number' || Number.isNaN(index) || index < 0) {
            index = 0;
        }

        let value = Math.floor(index) + 1;
        let prefix = '';

        while (value > 0) {
            value -= 1;
            const remainder = value % 26;
            prefix = String.fromCharCode(65 + remainder) + prefix;
            value = Math.floor(value / 26);
        }

        return prefix || 'A';
    }

    function buildSerializable(state) {
        return state.workflows.map((workflow) => ({
            id: workflow.id,
            title: normalizeString(workflow.title, 150),
            steps: workflow.steps.map((step) => ({
                id: step.id,
                sequenceCode: normalizeString(step.sequenceCode, 20).toUpperCase(),
                description: normalizeString(step.description, 500),
                stepType: step.stepType === STEP_TYPE_DECISION ? STEP_TYPE_DECISION : STEP_TYPE_NORMAL,
                role: normalizeString(step.role, 150),
                nextStepCode: (step.stepType === STEP_TYPE_DECISION ? '' : normalizeStepCode(step.nextStepCode)) || null,
                nextStepYesCode: (step.stepType === STEP_TYPE_DECISION ? normalizeStepCode(step.nextStepYesCode) : '') || null,
                nextStepNoCode: (step.stepType === STEP_TYPE_DECISION ? normalizeStepCode(step.nextStepNoCode) : '') || null
            }))
        }));
    }

    function nextSequenceCode(workflow, prefix) {
        const normalizedPrefix = typeof prefix === 'string' && prefix.trim()
            ? prefix.trim().toUpperCase()
            : 'A';

        const existing = new Set();
        workflow.steps.forEach((step) => {
            const code = (step.sequenceCode || '').toUpperCase();
            if (!code.startsWith(normalizedPrefix)) {
                return;
            }

            const suffix = code.substring(normalizedPrefix.length);
            const number = Number.parseInt(suffix, 10);
            if (!Number.isNaN(number)) {
                existing.add(number);
            }
        });

        let index = 1;
        while (existing.has(index)) {
            index += 1;
        }

        return `${normalizedPrefix}${index}`;
    }

    function createElement(tag, className, textContent) {
        const element = document.createElement(tag);
        if (className) {
            element.className = className;
        }
        if (typeof textContent === 'string') {
            element.textContent = textContent;
        }
        return element;
    }

    function initWorkflowDesigner(root) {
        const canEdit = root.getAttribute('data-can-edit') === 'true';
        const initialJson = root.getAttribute('data-initial');
        const listContainer = root.querySelector('[data-workflow-list]');
        const addButton = root.querySelector('[data-workflow-add]');
        const emptyMessage = root.getAttribute('data-empty') || 'Henüz iş akışı tanımlanmamış.';
        const inputSelector = root.getAttribute('data-input');
        const hiddenInput = inputSelector ? document.querySelector(inputSelector) : null;
        const saveButton = hiddenInput ? document.querySelector('[data-workflow-save]') : null;

        if (!listContainer) {
            return;
        }

        const state = {
            workflows: parseInitialWorkflows(initialJson)
        };

        const focusQueue = [];

        function queueFocus(selector) {
            focusQueue.push(selector);
        }

        function applyFocusQueue(card) {
            while (focusQueue.length > 0) {
                const selector = focusQueue.shift();
                const target = selector ? card.querySelector(selector) : null;
                if (target) {
                    target.focus();
                    break;
                }
            }
        }

        function updateHiddenField() {
            if (!hiddenInput) {
                return;
            }

            const serializable = buildSerializable(state);
            hiddenInput.value = JSON.stringify({ workflows: serializable });

            if (saveButton) {
                const isValid = serializable.length > 0 && serializable.every((workflow) => workflow.title && workflow.steps.length > 0 && workflow.steps.every((step) => step.description));
                saveButton.disabled = !isValid;
            }
        }

        function moveStep(workflow, index, direction) {
            const target = index + direction;
            if (target < 0 || target >= workflow.steps.length) {
                return;
            }

            const [step] = workflow.steps.splice(index, 1);
            workflow.steps.splice(target, 0, step);
            render();
        }

        function removeWorkflow(index) {
            if (index < 0 || index >= state.workflows.length) {
                return;
            }

            state.workflows.splice(index, 1);
            render();
        }

        function removeStep(workflow, index) {
            if (index < 0 || index >= workflow.steps.length) {
                return;
            }

            workflow.steps.splice(index, 1);
            render();
        }

        function addStep(workflow) {
            const workflowIndex = state.workflows.indexOf(workflow);
            const prefix = getSequencePrefixForIndex(workflowIndex < 0 ? 0 : workflowIndex);
            const code = nextSequenceCode(workflow, prefix);
            workflow.steps.push({
                id: null,
                sequenceCode: code,
                description: '',
                stepType: STEP_TYPE_NORMAL,
                role: '',
                nextStepCode: '',
                nextStepYesCode: '',
                nextStepNoCode: ''
            });
            queueFocus('textarea');
            render();
        }

        function addWorkflow() {
            const workflow = {
                id: null,
                title: 'Yeni İş Akışı',
                steps: []
            };
            state.workflows.push(workflow);
            queueFocus("input[data-workflow-title]");
            render();
        }

        function render() {
            listContainer.innerHTML = '';

            if (state.workflows.length === 0) {
                listContainer.appendChild(createElement('p', 'yardim-metin', emptyMessage));
                updateHiddenField();
                return;
            }

            state.workflows.forEach((workflow, workflowIndex) => {
                const card = createElement('article', 'is-akisi-kart');
                const prefix = getSequencePrefixForIndex(workflowIndex);

                const header = createElement('header', 'is-akisi-baslik');
                card.appendChild(header);

                if (canEdit) {
                    const titleInput = document.createElement('input');
                    titleInput.type = 'text';
                    titleInput.value = workflow.title;
                    titleInput.placeholder = 'İş akışı adı';
                    titleInput.setAttribute('data-workflow-title', '');
                    titleInput.addEventListener('input', () => {
                        workflow.title = titleInput.value;
                        updateHiddenField();
                    });
                    header.appendChild(titleInput);

                    const deleteButton = createElement('button', 'buton-link ikincil', 'Akışı Sil');
                    deleteButton.type = 'button';
                    deleteButton.addEventListener('click', () => {
                        if (window.confirm('Bu iş akışını silmek istediğinizden emin misiniz?')) {
                            removeWorkflow(workflowIndex);
                        }
                    });
                    header.appendChild(deleteButton);
                }
                else {
                    const title = createElement('h3', null, workflow.title);
                    header.appendChild(title);
                }

                const table = createElement('table', 'is-akisi-tablo');
                const thead = document.createElement('thead');
                const headerRow = document.createElement('tr');
                ['Sıra', 'Açıklama', 'Rol', 'Tür', 'Geçişler', 'İşlemler'].forEach((title) => {
                    headerRow.appendChild(createElement('th', null, title));
                });
                thead.appendChild(headerRow);
                table.appendChild(thead);

                const tbody = document.createElement('tbody');
                table.appendChild(tbody);

                workflow.steps.forEach((step, stepIndex) => {
                    const row = document.createElement('tr');
                    row.className = step.description.trim() ? '' : 'gecersiz';

                    const codeCell = createElement('td', 'is-akisi-sira', step.sequenceCode || `${prefix}${stepIndex + 1}`);
                    row.appendChild(codeCell);

                    const descriptionCell = document.createElement('td');
                    if (canEdit) {
                        const textarea = document.createElement('textarea');
                        textarea.rows = 2;
                        textarea.value = step.description;
                        textarea.placeholder = 'Adım açıklaması';
                        textarea.addEventListener('input', () => {
                            step.description = textarea.value;
                            if (step.description.trim()) {
                                row.classList.remove('gecersiz');
                            }
                            else {
                                row.classList.add('gecersiz');
                            }
                            updateHiddenField();
                        });
                        descriptionCell.appendChild(textarea);
                    }
                    else {
                        descriptionCell.textContent = step.description;
                    }
                    row.appendChild(descriptionCell);

                    const roleCell = document.createElement('td');
                    if (canEdit) {
                        const input = document.createElement('input');
                        input.type = 'text';
                        input.value = step.role;
                        input.placeholder = 'Rol';
                        input.addEventListener('input', () => {
                            step.role = input.value;
                            updateHiddenField();
                        });
                        roleCell.appendChild(input);
                    }
                    else {
                        roleCell.textContent = step.role || '-';
                    }
                    row.appendChild(roleCell);

                    const typeCell = document.createElement('td');
                    if (canEdit) {
                        const select = document.createElement('select');
                        const normalOption = createElement('option', null, 'Normal');
                        normalOption.value = STEP_TYPE_NORMAL;
                        const decisionOption = createElement('option', null, 'Karar Noktası');
                        decisionOption.value = STEP_TYPE_DECISION;
                        select.appendChild(normalOption);
                        select.appendChild(decisionOption);
                        select.value = step.stepType === STEP_TYPE_DECISION ? STEP_TYPE_DECISION : STEP_TYPE_NORMAL;
                        select.addEventListener('change', () => {
                            const previousType = step.stepType;
                            const newType = select.value === STEP_TYPE_DECISION ? STEP_TYPE_DECISION : STEP_TYPE_NORMAL;
                            step.stepType = newType;
                            if (newType === STEP_TYPE_DECISION && previousType !== STEP_TYPE_DECISION) {
                                step.nextStepYesCode = step.nextStepCode || step.nextStepYesCode || '';
                                step.nextStepNoCode = step.nextStepNoCode || '';
                                step.nextStepCode = '';
                                queueFocus('[data-branch-yes]');
                            }
                            else if (newType === STEP_TYPE_NORMAL && previousType === STEP_TYPE_DECISION) {
                                step.nextStepCode = step.nextStepYesCode || step.nextStepNoCode || step.nextStepCode || '';
                                step.nextStepYesCode = '';
                                step.nextStepNoCode = '';
                                queueFocus('input[data-next-step]');
                            }
                            updateHiddenField();
                            render();
                        });
                        typeCell.appendChild(select);
                    }
                    else {
                        typeCell.textContent = step.stepType === STEP_TYPE_DECISION ? 'Karar Noktası' : 'Normal';
                    }
                    row.appendChild(typeCell);

                    const nextCell = document.createElement('td');
                    nextCell.className = 'is-akisi-gecis';
                    if (canEdit) {
                        if (step.stepType === STEP_TYPE_DECISION) {
                            const yesWrapper = createElement('div', 'is-akisi-gecis-satir');
                            const yesLabel = createElement('span', 'is-akisi-gecis-etiket', 'Evet Adımı');
                            const yesInput = document.createElement('input');
                            yesInput.type = 'text';
                            yesInput.value = step.nextStepYesCode || '';
                            yesInput.placeholder = 'Evet seçeneği';
                            yesInput.setAttribute('data-branch-yes', '');
                            yesInput.addEventListener('input', () => {
                                step.nextStepYesCode = yesInput.value;
                                updateHiddenField();
                            });
                            yesWrapper.appendChild(yesLabel);
                            yesWrapper.appendChild(yesInput);

                            const noWrapper = createElement('div', 'is-akisi-gecis-satir');
                            const noLabel = createElement('span', 'is-akisi-gecis-etiket', 'Hayır Adımı');
                            const noInput = document.createElement('input');
                            noInput.type = 'text';
                            noInput.value = step.nextStepNoCode || '';
                            noInput.placeholder = 'Hayır seçeneği';
                            noInput.addEventListener('input', () => {
                                step.nextStepNoCode = noInput.value;
                                updateHiddenField();
                            });
                            noWrapper.appendChild(noLabel);
                            noWrapper.appendChild(noInput);

                            nextCell.appendChild(yesWrapper);
                            nextCell.appendChild(noWrapper);
                        }
                        else {
                            const input = document.createElement('input');
                            input.type = 'text';
                            input.value = step.nextStepCode || '';
                            input.placeholder = 'Sonraki adım';
                            input.setAttribute('data-next-step', '');
                            input.addEventListener('input', () => {
                                step.nextStepCode = input.value;
                                updateHiddenField();
                            });
                            nextCell.appendChild(input);
                        }
                    }
                    else if (step.stepType === STEP_TYPE_DECISION) {
                        const yesText = step.nextStepYesCode ? `Evet: ${step.nextStepYesCode}` : 'Evet: -';
                        const noText = step.nextStepNoCode ? `Hayır: ${step.nextStepNoCode}` : 'Hayır: -';
                        nextCell.appendChild(createElement('div', 'is-akisi-sonraki', yesText));
                        nextCell.appendChild(createElement('div', 'is-akisi-sonraki', noText));
                    }
                    else {
                        nextCell.textContent = step.nextStepCode || '-';
                    }
                    row.appendChild(nextCell);

                    const actionsCell = document.createElement('td');
                    actionsCell.className = 'is-akisi-islem';
                    if (canEdit) {
                        const upButton = createElement('button', 'buton-link kucuk', 'Yukarı');
                        upButton.type = 'button';
                        upButton.disabled = stepIndex === 0;
                        upButton.addEventListener('click', () => moveStep(workflow, stepIndex, -1));
                        actionsCell.appendChild(upButton);

                        const downButton = createElement('button', 'buton-link kucuk', 'Aşağı');
                        downButton.type = 'button';
                        downButton.disabled = stepIndex === workflow.steps.length - 1;
                        downButton.addEventListener('click', () => moveStep(workflow, stepIndex, 1));
                        actionsCell.appendChild(downButton);

                        const deleteButton = createElement('button', 'buton-link ikincil kucuk', 'Sil');
                        deleteButton.type = 'button';
                        deleteButton.addEventListener('click', () => removeStep(workflow, stepIndex));
                        actionsCell.appendChild(deleteButton);
                    }
                    row.appendChild(actionsCell);

                    tbody.appendChild(row);
                });

                card.appendChild(table);

                if (canEdit) {
                    const footer = createElement('div', 'is-akisi-alt');
                    const addStepButton = createElement('button', 'buton-link', 'Yeni Adım Ekle');
                    addStepButton.type = 'button';
                    addStepButton.addEventListener('click', () => addStep(workflow));
                    footer.appendChild(addStepButton);
                    card.appendChild(footer);
                }

                listContainer.appendChild(card);
                applyFocusQueue(card);
            });

            updateHiddenField();
        }

        if (canEdit && addButton) {
            addButton.addEventListener('click', (event) => {
                event.preventDefault();
                addWorkflow();
            });
        }

        render();
    }

    function initNotifications() {
        const root = document.querySelector('[data-notification-root]');
        if (!root) {
            return;
        }

        const toggle = root.querySelector('[data-notification-toggle]');
        const panel = root.querySelector('[data-notification-panel]');
        const countBadge = root.querySelector('[data-notification-count]');
        const csrfToken = getCsrfToken();

        const getItems = () => Array.from(root.querySelectorAll('[data-notification-item]'));

        toggle?.addEventListener('click', (event) => {
            event.preventDefault();
            panel?.classList.toggle('acik');
        });

        document.addEventListener('click', (event) => {
            if (!root.contains(event.target)) {
                panel?.classList.remove('acik');
            }
        });

        getItems().forEach((item) => {
            item.addEventListener('click', async () => {
                const id = item.getAttribute('data-notification-id');
                const link = item.getAttribute('data-notification-link');

                if (id) {
                    try {
                        await fetch('/Notifications/Okundu', {
                            method: 'POST',
                            headers: {
                                'Content-Type': 'application/json',
                                'RequestVerificationToken': csrfToken
                            },
                            body: JSON.stringify({ id: Number(id) })
                        });
                    }
                    catch (error) {
                        console.warn('Bildirim güncellemesi sırasında hata oluştu', error);
                    }
                }

                item.remove();
                const remaining = getItems().length;
                if (countBadge) {
                    countBadge.textContent = remaining > 0 ? remaining.toString() : '';
                }

                if (link) {
                    window.location.href = link;
                }
            });
        });
    }

    document.addEventListener('DOMContentLoaded', () => {
        document.querySelectorAll('[data-workflow-designer]').forEach((element) => initWorkflowDesigner(element));
        initNotifications();
    });
})();
