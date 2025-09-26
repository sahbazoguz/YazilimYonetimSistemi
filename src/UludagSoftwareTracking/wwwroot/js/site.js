(function () {
    function getCsrfToken() {
        const meta = document.querySelector('meta[name="csrf-token"]');
        return meta ? meta.getAttribute('content') || '' : '';
    }

    const STEP_TYPE_NORMAL = 'Normal';
    const STEP_TYPE_DECISION = 'Decision';

    function normalizeStepType(type) {
        if (typeof type === 'string' && type.toLowerCase() === STEP_TYPE_DECISION.toLowerCase()) {
            return STEP_TYPE_DECISION;
        }

        return STEP_TYPE_NORMAL;
    }

    function cloneNextValue(next) {
        if (next === undefined || next === null) {
            return null;
        }

        try {
            return JSON.parse(JSON.stringify(next));
        } catch (error) {
            console.warn('Karar dalları kopyalanamadı:', error);
            return null;
        }
    }

    function extractBranch(target, keys) {
        if (!target || typeof target !== 'object') {
            return null;
        }

        for (const key of keys) {
            if (Object.prototype.hasOwnProperty.call(target, key) && target[key] !== undefined) {
                return target[key];
            }
        }

        return null;
    }

    function parseSteps(json) {
        if (!json) {
            return [];
        }

        try {
            const parsed = JSON.parse(json);
            if (Array.isArray(parsed)) {
                return parsed.map((step, index) => {
                    const type = normalizeStepType(step?.Type ?? step?.type);
                    const role = typeof step?.Role === 'string'
                        ? step.Role
                        : (typeof step?.role === 'string' ? step.role : '');
                    const description = typeof step?.Description === 'string'
                        ? step.Description
                        : (typeof step?.description === 'string' ? step.description : '');

                    const nextValue = step?.Next ?? step?.next;

                    return {
                        Code: typeof step?.Code === 'string'
                            ? step.Code
                            : (typeof step?.code === 'string' ? step.code : `A${index + 1}`),
                        Type: type,
                        Title: typeof step?.Title === 'string' ? step.Title : (typeof step?.title === 'string' ? step.title : ''),
                        Description: description,
                        Role: role,
                        Next: cloneNextValue(nextValue)
                    };
                });
            }
        } catch (error) {
            console.warn('Algoritma adımları çözümlenemedi:', error);
        }

        return [];
    }

    function renderFlowPreview(container, steps) {
        if (!container) {
            return;
        }

        container.innerHTML = '';

        if (!Array.isArray(steps) || steps.length === 0) {
            const empty = document.createElement('p');
            empty.className = 'yardim-metin';
            empty.textContent = 'Henüz adım eklenmedi.';
            container.appendChild(empty);
            return;
        }

        steps.forEach((step, index) => {
            const card = document.createElement('div');
            card.className = 'akisma-kart';

            const title = document.createElement('div');
            title.className = 'akisma-kart-baslik';
            const stepType = normalizeStepType(step.Type);
            const codeText = step.Code && step.Code.length > 0 ? step.Code : `A${index + 1}`;
            const stepTitle = step.Title && step.Title.trim().length > 0 ? ` - ${step.Title.trim()}` : '';
            title.textContent = `${codeText}${stepTitle}`;
            card.appendChild(title);

            if (stepType === STEP_TYPE_DECISION) {
                const typeBadge = document.createElement('span');
                typeBadge.className = 'akisma-kart-tur';
                typeBadge.textContent = 'Karar Noktası';
                card.appendChild(typeBadge);
            }

            if (step.Role && step.Role.toString().trim().length > 0) {
                const roleBadge = document.createElement('span');
                roleBadge.className = 'akisma-kart-rol';
                roleBadge.textContent = step.Role.toString().trim();
                card.appendChild(roleBadge);
            }

            if (step.Description) {
                const desc = document.createElement('p');
                desc.className = 'akisma-kart-icerik';
                desc.textContent = step.Description;
                card.appendChild(desc);
            }

            if (stepType === STEP_TYPE_DECISION && step.Next && typeof step.Next === 'object' && !Array.isArray(step.Next)) {
                const branchesWrapper = document.createElement('div');
                branchesWrapper.className = 'akisma-kart-dallar';

                const yesValue = extractBranch(step.Next, ['Evet', 'evet']);
                const noValue = extractBranch(step.Next, ['Hayır', 'Hayir', 'hayir']);

                const yesItem = document.createElement('span');
                yesItem.className = 'akisma-kart-dal';
                const yesText = typeof yesValue === 'string' && yesValue.trim().length > 0
                    ? yesValue.trim()
                    : (yesValue ? JSON.stringify(yesValue) : '—');
                yesItem.innerHTML = `<strong>Evet:</strong> ${yesText}`;
                branchesWrapper.appendChild(yesItem);

                const noItem = document.createElement('span');
                noItem.className = 'akisma-kart-dal';
                const noText = typeof noValue === 'string' && noValue.trim().length > 0
                    ? noValue.trim()
                    : (noValue ? JSON.stringify(noValue) : '—');
                noItem.innerHTML = `<strong>Hayır:</strong> ${noText}`;
                branchesWrapper.appendChild(noItem);

                card.appendChild(branchesWrapper);
            }

            container.appendChild(card);

            if (index < steps.length - 1) {
                const arrow = document.createElement('div');
                arrow.className = 'akisma-ok';
                arrow.innerHTML = '&#8595;';
                container.appendChild(arrow);
            }
        });
    }

    function initFlowEditor(form) {
        const hiddenInput = form.querySelector('[data-flow-input]');
        const editor = form.querySelector('[data-flow-editor]');
        const list = editor?.querySelector('[data-flow-list]');
        const addButton = editor?.querySelector('[data-flow-add]');

        if (!hiddenInput || !editor || !list || !addButton) {
            return;
        }

        let steps = parseSteps(hiddenInput.value);
        const previewSelector = form.getAttribute('data-flow-target');
        const previewElement = previewSelector ? document.querySelector(previewSelector) : null;

        function refreshCodes() {
            steps.forEach((step, index) => {
                step.Code = `A${index + 1}`;
            });
        }

        function refreshNextLinks() {
            steps.forEach((step, index) => {
                const normalizedType = normalizeStepType(step.Type);
                step.Type = normalizedType;

                if (normalizedType === STEP_TYPE_DECISION) {
                    const currentNext = step.Next && typeof step.Next === 'object' && !Array.isArray(step.Next)
                        ? step.Next
                        : {};

                    const yesValue = extractBranch(currentNext, ['Evet', 'evet']);
                    const noValue = extractBranch(currentNext, ['Hayır', 'Hayir', 'hayir']);

                    const yesBranch = cloneNextValue(yesValue);
                    const noBranch = cloneNextValue(noValue);

                    step.Next = {
                        Evet: yesBranch !== null ? yesBranch : (index < steps.length - 1 ? steps[index + 1].Code : null),
                        'Hayır': noBranch
                    };
                } else {
                    if (step.Next === null || step.Next === undefined ||
                        (typeof step.Next === 'string' && step.Next.trim().length === 0)) {
                        step.Next = index < steps.length - 1 ? steps[index + 1].Code : null;
                    } else if (typeof step.Next === 'string') {
                        step.Next = step.Next.trim();
                    } else if (typeof step.Next === 'object') {
                        step.Next = cloneNextValue(step.Next);
                    }
                }
            });
        }

        function syncState() {
            refreshCodes();
            refreshNextLinks();
            const serialized = steps.map((step) => {
                const title = (step.Title || '').trim();
                const description = (step.Description || '').trim();
                const role = (step.Role || '').trim();
                const type = normalizeStepType(step.Type);
                step.Type = type;
                step.Title = title;
                step.Role = role;

                return {
                    Code: step.Code || '',
                    Type: type,
                    Title: title,
                    Description: description,
                    Role: role,
                    Next: cloneNextValue(step.Next)
                };
            });
            hiddenInput.value = JSON.stringify(serialized);
            if (previewElement) {
                previewElement.setAttribute('data-flow-source', hiddenInput.value);
                renderFlowPreview(previewElement, serialized);
            }
        }

        function renderList() {
            list.innerHTML = '';
            refreshCodes();
            refreshNextLinks();

            if (steps.length === 0) {
                const empty = document.createElement('li');
                empty.className = 'akisma-editor-bos';
                empty.textContent = 'Henüz adım bulunmuyor. Yeni adım ekleyin.';
                list.appendChild(empty);
                return;
            }

            steps.forEach((step, index) => {
                step.Type = normalizeStepType(step.Type);
                step.Role = typeof step.Role === 'string' ? step.Role : '';

                const item = document.createElement('li');
                item.className = 'akisma-editor-adim';
                item.setAttribute('draggable', 'true');
                item.dataset.index = index.toString();

                const handle = document.createElement('span');
                handle.className = 'akisma-editor-tutamak';
                handle.innerHTML = '&#9776;';
                item.appendChild(handle);

                const codeBadge = document.createElement('span');
                codeBadge.className = 'akisma-editor-kod';
                codeBadge.textContent = step.Code || `A${index + 1}`;
                item.appendChild(codeBadge);

                const fields = document.createElement('div');
                fields.className = 'akisma-editor-icerik';

                const typeSelect = document.createElement('select');
                typeSelect.className = 'form-control akisma-editor-tur';
                typeSelect.setAttribute('aria-label', 'Adım türü');
                [
                    { value: STEP_TYPE_NORMAL, label: 'Normal Adım' },
                    { value: STEP_TYPE_DECISION, label: 'Karar Noktası' }
                ].forEach((optionDef) => {
                    const option = document.createElement('option');
                    option.value = optionDef.value;
                    option.textContent = optionDef.label;
                    typeSelect.appendChild(option);
                });
                typeSelect.value = step.Type;
                typeSelect.addEventListener('change', () => {
                    step.Type = typeSelect.value;
                    syncState();
                    renderList();
                });
                fields.appendChild(typeSelect);

                const titleInput = document.createElement('input');
                titleInput.type = 'text';
                titleInput.className = 'form-control akisma-editor-baslik';
                titleInput.placeholder = 'Adım başlığı';
                titleInput.value = step.Title || '';
                titleInput.addEventListener('input', () => {
                    step.Title = titleInput.value;
                    syncState();
                });
                fields.appendChild(titleInput);

                const descriptionInput = document.createElement('textarea');
                descriptionInput.className = 'form-control akisma-editor-aciklama';
                descriptionInput.placeholder = 'Açıklama (isteğe bağlı)';
                descriptionInput.rows = 2;
                descriptionInput.value = step.Description || '';
                descriptionInput.addEventListener('input', () => {
                    step.Description = descriptionInput.value;
                    syncState();
                });
                fields.appendChild(descriptionInput);

                const roleInput = document.createElement('input');
                roleInput.type = 'text';
                roleInput.className = 'form-control akisma-editor-rol';
                roleInput.placeholder = 'Rol / sorumlu kişi (isteğe bağlı)';
                roleInput.value = step.Role || '';
                roleInput.addEventListener('input', () => {
                    step.Role = roleInput.value;
                    syncState();
                });
                fields.appendChild(roleInput);

                item.appendChild(fields);

                const removeButton = document.createElement('button');
                removeButton.type = 'button';
                removeButton.className = 'buton-link akisma-editor-sil';
                removeButton.textContent = 'Sil';
                removeButton.addEventListener('click', () => {
                    steps.splice(index, 1);
                    renderList();
                    syncState();
                });
                item.appendChild(removeButton);

                item.addEventListener('dragstart', (event) => {
                    event.dataTransfer?.setData('text/plain', index.toString());
                    event.dataTransfer?.setDragImage(handle, 10, 10);
                    item.classList.add('surukleniyor');
                });

                item.addEventListener('dragend', () => {
                    item.classList.remove('surukleniyor');
                });

                item.addEventListener('dragover', (event) => {
                    event.preventDefault();
                    item.classList.add('suruk-hedef');
                });

                item.addEventListener('dragleave', () => {
                    item.classList.remove('suruk-hedef');
                });

                item.addEventListener('drop', (event) => {
                    event.preventDefault();
                    item.classList.remove('suruk-hedef');
                    const fromIndex = Number(event.dataTransfer?.getData('text/plain'));
                    const toIndex = Number(item.dataset.index);
                    if (!Number.isNaN(fromIndex) && !Number.isNaN(toIndex) && fromIndex !== toIndex) {
                        const [moved] = steps.splice(fromIndex, 1);
                        steps.splice(toIndex, 0, moved);
                        renderList();
                        syncState();
                    }
                });

                list.appendChild(item);
            });
        }

        addButton.addEventListener('click', () => {
            if (addButton.disabled) {
                return;
            }
            steps.push({ Title: '', Description: '', Role: '', Type: STEP_TYPE_NORMAL, Next: null });
            renderList();
            syncState();
        });

        form.addEventListener('submit', (event) => {
            refreshCodes();
            const hasEmpty = steps.length > 0 && steps.some((step) => !(step.Title && step.Title.trim().length > 0));
            if (hasEmpty) {
                event.preventDefault();
                window.alert('Lütfen tüm algoritma adımları için başlık girin.');
                return;
            }
            syncState();
        });

        renderList();
        syncState();
    }

    document.addEventListener('DOMContentLoaded', () => {
        document.querySelectorAll('[data-flow-preview]').forEach((container) => {
            const source = container.getAttribute('data-flow-source') || '[]';
            const steps = parseSteps(source);
            renderFlowPreview(container, steps);
        });

        document.querySelectorAll('[data-flow-form]').forEach((form) => initFlowEditor(form));

        initNotifications();
    });

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
})();
