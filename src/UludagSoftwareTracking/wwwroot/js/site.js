(function () {
    const STEP_TYPES = {
        NORMAL: 'Normal',
        DECISION: 'Decision'
    };
    let mermaidInitialized = false;

    function getCsrfToken() {
        const meta = document.querySelector('meta[name="csrf-token"]');
        return meta ? meta.getAttribute('content') || '' : '';
    }

    function ensureMermaid() {
        const mermaid = window.mermaid;
        if (!mermaid || typeof mermaid.initialize !== 'function') {
            return null;
        }

        if (!mermaidInitialized) {
            mermaid.initialize({
                startOnLoad: false,
                theme: 'default',
                flowchart: {
                    curve: 'basis',
                    htmlLabels: true,
                    nodeSpacing: 60,
                    rankSpacing: 80
                }
            });
            mermaidInitialized = true;
        }

        return mermaid;
    }

    function escapeMermaid(text) {
        return (text || '')
            .replace(/\\/g, '\\\\')
            .replace(/\"/g, '\\\"')
            .replace(/\r?\n/g, '<br/>');
    }

    function sanitizeBranch(entry, index) {
        const labelSource = entry?.label ?? entry?.Label ?? `Seçenek ${index + 1}`;
        const targetSource = entry?.targetCode ?? entry?.TargetCode ?? '';
        const label = typeof labelSource === 'string' ? labelSource.trim() : `Seçenek ${index + 1}`;
        const targetCode = typeof targetSource === 'string' ? targetSource.trim().toUpperCase() : '';

        return {
            label,
            targetCode
        };
    }

    function sanitizeStep(entry, index) {
        const codeSource = entry?.code ?? entry?.Code ?? '';
        const titleSource = entry?.title ?? entry?.Title ?? '';
        const typeSource = entry?.type ?? entry?.Type ?? STEP_TYPES.NORMAL;
        const descriptionSource = entry?.description ?? entry?.Description ?? '';
        const roleSource = entry?.role ?? entry?.Role ?? '';
        const nextCodeSource = entry?.nextCode ?? entry?.NextCode ?? '';
        const legacyNext = entry?.next ?? entry?.Next ?? null;
        const branchesSource = entry?.branches ?? entry?.Branches ?? null;

        const code = typeof codeSource === 'string' && codeSource.trim().length > 0
            ? codeSource.trim().toUpperCase()
            : `A${index + 1}`;
        const title = typeof titleSource === 'string' ? titleSource.trim() : '';
        const type = typeof typeSource === 'string' && typeSource.toLowerCase() === STEP_TYPES.DECISION.toLowerCase()
            ? STEP_TYPES.DECISION
            : STEP_TYPES.NORMAL;
        const description = typeof descriptionSource === 'string' ? descriptionSource.trim() : '';
        const role = typeof roleSource === 'string' ? roleSource.trim() : '';

        let nextCode = '';
        if (type === STEP_TYPES.NORMAL) {
            if (typeof nextCodeSource === 'string' && nextCodeSource.trim().length > 0) {
                nextCode = nextCodeSource.trim().toUpperCase();
            }
            else if (typeof legacyNext === 'string' && legacyNext.trim().length > 0) {
                nextCode = legacyNext.trim().toUpperCase();
            }
            else if (Array.isArray(legacyNext) && legacyNext.length > 0) {
                const first = legacyNext.find((value) => typeof value === 'string' && value.trim().length > 0);
                if (first) {
                    nextCode = first.trim().toUpperCase();
                }
            }
        }

        let branches = [];
        if (type === STEP_TYPES.DECISION) {
            if (Array.isArray(branchesSource)) {
                branches = branchesSource
                    .map((branch, branchIndex) => sanitizeBranch(branch, branchIndex))
                    .filter((branch) => branch.label.length > 0 && branch.targetCode.length > 0);
            }
            else if (legacyNext && typeof legacyNext === 'object' && !Array.isArray(legacyNext)) {
                branches = Object.entries(legacyNext)
                    .map(([label, value], branchIndex) => ({
                        label: typeof label === 'string' ? label.trim() : `Seçenek ${branchIndex + 1}`,
                        targetCode: typeof value === 'string' ? value.trim().toUpperCase() : ''
                    }))
                    .filter((branch) => branch.label.length > 0 && branch.targetCode.length > 0);
            }
            else if (Array.isArray(legacyNext)) {
                branches = legacyNext
                    .map((value, branchIndex) => ({
                        label: `Seçenek ${branchIndex + 1}`,
                        targetCode: typeof value === 'string' ? value.trim().toUpperCase() : ''
                    }))
                    .filter((branch) => branch.targetCode.length > 0);
            }
            else if (typeof legacyNext === 'string' && legacyNext.trim().length > 0) {
                branches = [{ label: 'Evet', targetCode: legacyNext.trim().toUpperCase() }];
            }
        }

        return {
            code,
            title,
            type,
            description,
            role,
            nextCode,
            branches
        };
    }

    function sanitizeFlow(entry, index) {
        const nameSource = entry?.name ?? entry?.Name ?? '';
        const startSource = entry?.startCode ?? entry?.StartCode ?? '';

        const name = typeof nameSource === 'string' && nameSource.trim().length > 0
            ? nameSource.trim()
            : `Akış ${index + 1}`;
        const startCode = typeof startSource === 'string' ? startSource.trim().toUpperCase() : '';

        if (!startCode) {
            return null;
        }

        return {
            name,
            startCode
        };
    }

    function sanitizeAlgorithm(raw) {
        const candidate = raw && typeof raw === 'object' ? raw : {};
        const rawFlows = Array.isArray(candidate.flows)
            ? candidate.flows
            : (Array.isArray(candidate.Flows) ? candidate.Flows : []);
        const rawSteps = Array.isArray(candidate.steps)
            ? candidate.steps
            : (Array.isArray(candidate.Steps) ? candidate.Steps : []);

        const steps = [];
        const seenCodes = new Set();

        rawSteps.forEach((entry, index) => {
            const sanitized = sanitizeStep(entry, index);
            if (!sanitized.title || seenCodes.has(sanitized.code)) {
                return;
            }

            seenCodes.add(sanitized.code);
            steps.push(sanitized);
        });

        const codes = new Set(steps.map((step) => step.code));
        steps.forEach((step) => {
            if (step.type === STEP_TYPES.NORMAL) {
                if (!step.nextCode || !codes.has(step.nextCode)) {
                    step.nextCode = '';
                }
                step.branches = [];
            }
            else {
                step.nextCode = '';
                step.branches = step.branches.filter((branch) => branch.label && branch.targetCode && codes.has(branch.targetCode));
            }
        });

        const flows = [];
        const usedNames = new Set();
        rawFlows.forEach((entry, index) => {
            const sanitized = sanitizeFlow(entry, index);
            if (!sanitized || !codes.has(sanitized.startCode)) {
                return;
            }

            let name = sanitized.name;
            let suffix = 2;
            while (usedNames.has(name.toLowerCase())) {
                name = `${sanitized.name} (${suffix})`;
                suffix += 1;
            }

            usedNames.add(name.toLowerCase());
            flows.push({ name, startCode: sanitized.startCode });
        });

        if (flows.length === 0 && steps.length > 0) {
            flows.push({ name: 'Ana Akış', startCode: steps[0].code });
        }

        return { flows, steps };
    }

    function parseAlgorithm(json) {
        if (!json) {
            return { flows: [], steps: [] };
        }

        try {
            const parsed = JSON.parse(json);
            return sanitizeAlgorithm(parsed);
        }
        catch (error) {
            console.warn('Algoritma JSON verisi okunamadı:', error);
            return { flows: [], steps: [] };
        }
    }

    function generateStepCode(steps) {
        let max = 0;
        steps.forEach((step) => {
            const match = /^A(\d+)$/i.exec(step.code);
            if (match) {
                const value = Number(match[1]);
                if (Number.isFinite(value)) {
                    max = Math.max(max, value);
                }
            }
        });
        return `A${max + 1}`;
    }
    function buildMermaidDefinition(flow, stepMap) {
        const visited = new Set();
        const queue = [flow.startCode];
        const nodes = [];
        const seenNodes = new Set();
        const edges = [];

        while (queue.length > 0) {
            const code = queue.shift();
            if (!code || visited.has(code)) {
                continue;
            }

            visited.add(code);
            const step = stepMap.get(code);
            if (!step) {
                continue;
            }

            if (!seenNodes.has(step.code)) {
                seenNodes.add(step.code);
                nodes.push(step);
            }

            if (step.type === STEP_TYPES.DECISION) {
                step.branches.forEach((branch) => {
                    if (!branch.targetCode) {
                        return;
                    }

                    edges.push({ from: step.code, to: branch.targetCode, label: branch.label });
                    queue.push(branch.targetCode);
                });
            }
            else if (step.nextCode) {
                edges.push({ from: step.code, to: step.nextCode, label: '' });
                queue.push(step.nextCode);
            }
        }

        if (nodes.length === 0) {
            return null;
        }

        const lines = ['graph TD'];
        nodes.forEach((step) => {
            const parts = [step.title || step.code];
            if (step.role) {
                parts.push(`Rol: ${step.role}`);
            }
            if (step.description) {
                parts.push(step.description);
            }
            const label = escapeMermaid(parts.join('\n'));
            if (step.type === STEP_TYPES.DECISION) {
                lines.push(`${step.code}{"${label}"}`);
            }
            else {
                lines.push(`${step.code}["${label}"]`);
            }
        });

        edges.forEach((edge) => {
            const label = edge.label ? `|${escapeMermaid(edge.label)}|` : '';
            lines.push(`${edge.from} -->${label} ${edge.to}`);
        });

        return lines.join('\n');
    }

    function renderAlgorithmDiagrams(container, algorithm) {
        if (!container) {
            return;
        }

        container.innerHTML = '';

        if (!algorithm || algorithm.steps.length === 0) {
            return;
        }

        const stepMap = new Map(algorithm.steps.map((step) => [step.code, step]));
        const mermaid = ensureMermaid();
        const hasRenderer = !!mermaid && typeof mermaid.render === 'function';
        let rendered = false;

        algorithm.flows.forEach((flow) => {
            const definition = buildMermaidDefinition(flow, stepMap);
            if (!definition) {
                return;
            }

            const wrapper = document.createElement('div');
            wrapper.className = 'algoritma-diagram-kapsul';
            const heading = document.createElement('h4');
            heading.textContent = flow.name;
            wrapper.appendChild(heading);

            if (!hasRenderer) {
                const pre = document.createElement('pre');
                pre.className = 'algoritma-diagram-kod';
                pre.textContent = definition;
                wrapper.appendChild(pre);
                container.appendChild(wrapper);
                rendered = true;
                return;
            }

            const host = document.createElement('div');
            host.className = 'algoritma-mermaid';
            wrapper.appendChild(host);
            container.appendChild(wrapper);

            const id = `mermaid-${Date.now()}-${Math.random().toString(36).slice(2)}`;
            mermaid.render(id, definition)
                .then(({ svg, bindFunctions }) => {
                    host.innerHTML = svg;
                    if (typeof bindFunctions === 'function') {
                        bindFunctions(host);
                    }
                })
                .catch((error) => {
                    console.warn('Mermaid diyagramı oluşturulamadı:', error);
                    const fallback = document.createElement('pre');
                    fallback.className = 'algoritma-diagram-kod';
                    fallback.textContent = definition;
                    host.replaceWith(fallback);
                });

            rendered = true;
        });

        if (!rendered) {
            const empty = document.createElement('p');
            empty.className = 'yardim-metin';
            empty.textContent = 'Diyagram oluşturmak için akış ve adımları tanımlayın.';
            container.appendChild(empty);
        }
    }

    function renderAlgorithmSummary(container, algorithm) {
        if (!container) {
            return;
        }

        container.innerHTML = '';

        if (!algorithm || algorithm.steps.length === 0) {
            return;
        }

        if (algorithm.flows.length > 0) {
            const flowList = document.createElement('ul');
            flowList.className = 'algoritma-ozet-akislar';
            algorithm.flows.forEach((flow) => {
                const item = document.createElement('li');
                item.textContent = `${flow.name} → ${flow.startCode}`;
                flowList.appendChild(item);
            });
            container.appendChild(flowList);
        }

        const stepList = document.createElement('ol');
        stepList.className = 'algoritma-ozet-adimlar';

        algorithm.steps.forEach((step) => {
            const item = document.createElement('li');

            const heading = document.createElement('span');
            heading.className = 'algoritma-ozet-baslik';
            heading.textContent = `${step.code} - ${step.title}`;
            item.appendChild(heading);

            const typeBadge = document.createElement('span');
            typeBadge.className = 'algoritma-ozet-tur';
            typeBadge.textContent = step.type === STEP_TYPES.DECISION ? 'Karar' : 'Normal';
            item.appendChild(typeBadge);

            if (step.role) {
                const roleBadge = document.createElement('span');
                roleBadge.className = 'algoritma-ozet-rol';
                roleBadge.textContent = step.role;
                item.appendChild(roleBadge);
            }

            if (step.description) {
                const description = document.createElement('p');
                description.textContent = step.description;
                item.appendChild(description);
            }

            if (step.type === STEP_TYPES.NORMAL && step.nextCode) {
                const nextInfo = document.createElement('small');
                nextInfo.className = 'algoritma-ozet-gecis';
                nextInfo.textContent = `Sonraki: ${step.nextCode}`;
                item.appendChild(nextInfo);
            }
            else if (step.type === STEP_TYPES.DECISION && step.branches.length > 0) {
                const branchList = document.createElement('ul');
                branchList.className = 'algoritma-ozet-dallar';
                step.branches.forEach((branch) => {
                    const branchItem = document.createElement('li');
                    branchItem.textContent = `${branch.label} → ${branch.targetCode}`;
                    branchList.appendChild(branchItem);
                });
                item.appendChild(branchList);
            }

            stepList.appendChild(item);
        });

        container.appendChild(stepList);
    }

    function updateDisplay(container, algorithm) {
        if (!container) {
            return;
        }

        const emptyState = container.querySelector('[data-algorithm-empty]');
        if (emptyState) {
            emptyState.style.display = algorithm.steps.length === 0 ? '' : 'none';
        }

        renderAlgorithmDiagrams(container.querySelector('[data-algorithm-diagrams]'), algorithm);
        renderAlgorithmSummary(container.querySelector('[data-algorithm-summary]'), algorithm);
        container.setAttribute('data-algorithm-json', JSON.stringify(algorithm));
    }

    function initDisplay(container) {
        const source = container.getAttribute('data-algorithm-json') || '';
        const algorithm = parseAlgorithm(source);
        updateDisplay(container, algorithm);
    }
    function toSerializable(state) {
        const codes = new Set(state.steps.map((step) => step.code));
        const flows = [];
        const usedNames = new Set();

        state.flows.forEach((flow) => {
            const baseName = (flow.name || '').trim();
            const startCode = (flow.startCode || '').trim().toUpperCase();
            if (!baseName || !codes.has(startCode)) {
                return;
            }

            let name = baseName;
            let index = 2;
            while (usedNames.has(name.toLowerCase())) {
                name = `${baseName} (${index})`;
                index += 1;
            }

            usedNames.add(name.toLowerCase());
            flows.push({ name, startCode });
        });

        if (flows.length === 0 && state.steps.length > 0) {
            flows.push({ name: 'Ana Akış', startCode: state.steps[0].code });
        }

        const steps = state.steps
            .map((step) => {
                const title = (step.title || '').trim();
                if (!title) {
                    return null;
                }

                const description = (step.description || '').trim();
                const role = (step.role || '').trim();
                const result = {
                    code: step.code,
                    type: step.type,
                    title,
                    description: description || undefined,
                    role: role || undefined,
                    nextCode: undefined,
                    branches: undefined
                };

                if (step.type === STEP_TYPES.NORMAL) {
                    const nextCode = (step.nextCode || '').trim().toUpperCase();
                    result.nextCode = nextCode && codes.has(nextCode) ? nextCode : undefined;
                    result.branches = [];
                }
                else {
                    const branches = step.branches
                        .map((branch) => {
                            const label = (branch.label || '').trim();
                            const target = (branch.targetCode || '').trim().toUpperCase();
                            if (!label || !codes.has(target)) {
                                return null;
                            }

                            return { label, targetCode: target };
                        })
                        .filter(Boolean);

                    result.branches = branches;
                }

                return result;
            })
            .filter(Boolean);

        return { flows, steps };
    }

    function initEditor(form) {
        const hiddenInput = form.querySelector('[data-algorithm-input]');
        const flowList = form.querySelector('[data-algorithm-flow-list]');
        const stepList = form.querySelector('[data-algorithm-step-list]');
        const addFlowButton = form.querySelector('[data-algorithm-add-flow]');
        const addStepButton = form.querySelector('[data-algorithm-add-step]');
        const diagramSelector = form.getAttribute('data-algorithm-diagram-target');
        const previewSelector = form.getAttribute('data-algorithm-preview');
        const diagramContainer = diagramSelector ? document.querySelector(diagramSelector) : form.querySelector('[data-algorithm-diagrams]');
        const previewContainer = previewSelector ? document.querySelector(previewSelector) : null;
        const readOnly = form.getAttribute('data-algorithm-readonly') === 'true';

        if (!hiddenInput || !flowList || !stepList) {
            return;
        }

        let state = parseAlgorithm(hiddenInput.value);

        function updateOutputs() {
            const serializable = toSerializable(state);
            hiddenInput.value = JSON.stringify(serializable);
            if (previewContainer) {
                updateDisplay(previewContainer, serializable);
            }
            if (diagramContainer) {
                renderAlgorithmDiagrams(diagramContainer, serializable);
            }
        }

        function renderFlows() {
            flowList.innerHTML = '';

            if (state.flows.length === 0) {
                const empty = document.createElement('p');
                empty.className = 'yardim-metin';
                empty.textContent = state.steps.length === 0
                    ? 'Önce adım ekleyin.'
                    : 'Başlangıç akışı ekleyin.';
                flowList.appendChild(empty);
                return;
            }

            state.flows.forEach((flow, index) => {
                const row = document.createElement('div');
                row.className = 'algoritma-flow-satir';

                const nameInput = document.createElement('input');
                nameInput.type = 'text';
                nameInput.value = flow.name;
                nameInput.placeholder = 'Akış adı';
                nameInput.disabled = readOnly;
                nameInput.addEventListener('input', () => {
                    flow.name = nameInput.value;
                    updateOutputs();
                });
                row.appendChild(nameInput);

                const startSelect = document.createElement('select');
                startSelect.disabled = readOnly || state.steps.length === 0;
                state.steps.forEach((step) => {
                    const option = document.createElement('option');
                    option.value = step.code;
                    option.textContent = `${step.code} - ${step.title || 'İsimsiz adım'}`;
                    startSelect.appendChild(option);
                });
                startSelect.value = flow.startCode;
                startSelect.addEventListener('change', () => {
                    flow.startCode = startSelect.value;
                    updateOutputs();
                });
                row.appendChild(startSelect);

                if (!readOnly) {
                    const removeButton = document.createElement('button');
                    removeButton.type = 'button';
                    removeButton.className = 'buton-link';
                    removeButton.textContent = 'Sil';
                    removeButton.addEventListener('click', () => {
                        state.flows.splice(index, 1);
                        render();
                    });
                    row.appendChild(removeButton);
                }

                flowList.appendChild(row);
            });
        }

        function renderBranchControls(step, container) {
            container.innerHTML = '';

            if (step.branches.length === 0) {
                const empty = document.createElement('p');
                empty.className = 'yardim-metin';
                empty.textContent = 'En az bir karar seçeneği ekleyin.';
                container.appendChild(empty);
                return;
            }

            step.branches.forEach((branch, branchIndex) => {
                const row = document.createElement('div');
                row.className = 'algoritma-branch-satir';

                const labelInput = document.createElement('input');
                labelInput.type = 'text';
                labelInput.placeholder = 'Seçenek etiketi';
                labelInput.value = branch.label;
                labelInput.disabled = readOnly;
                labelInput.addEventListener('input', () => {
                    branch.label = labelInput.value;
                    updateOutputs();
                });
                row.appendChild(labelInput);

                const targetSelect = document.createElement('select');
                targetSelect.disabled = readOnly || state.steps.length === 0;
                state.steps.forEach((candidate) => {
                    const option = document.createElement('option');
                    option.value = candidate.code;
                    option.textContent = `${candidate.code} - ${candidate.title || 'İsimsiz adım'}`;
                    targetSelect.appendChild(option);
                });
                targetSelect.value = branch.targetCode;
                targetSelect.addEventListener('change', () => {
                    branch.targetCode = targetSelect.value;
                    updateOutputs();
                });
                row.appendChild(targetSelect);

                if (!readOnly) {
                    const removeButton = document.createElement('button');
                    removeButton.type = 'button';
                    removeButton.className = 'buton-link';
                    removeButton.textContent = 'Sil';
                    removeButton.addEventListener('click', () => {
                        step.branches.splice(branchIndex, 1);
                        render();
                    });
                    row.appendChild(removeButton);
                }

                container.appendChild(row);
            });
        }

        function renderSteps() {
            stepList.innerHTML = '';

            if (state.steps.length === 0) {
                const empty = document.createElement('p');
                empty.className = 'yardim-metin';
                empty.textContent = 'Henüz adım eklenmedi.';
                stepList.appendChild(empty);
                return;
            }

            state.steps.forEach((step, index) => {
                const card = document.createElement('article');
                card.className = 'algoritma-step-kart';

                const header = document.createElement('header');
                header.className = 'algoritma-step-baslik';
                card.appendChild(header);

                const codeInput = document.createElement('input');
                codeInput.type = 'text';
                codeInput.value = step.code;
                codeInput.maxLength = 10;
                codeInput.disabled = readOnly;
                codeInput.addEventListener('blur', () => {
                    if (readOnly) {
                        return;
                    }

                    const newCode = codeInput.value.trim().toUpperCase();
                    if (!newCode) {
                        codeInput.value = step.code;
                        return;
                    }

                    if (state.steps.some((candidate, candidateIndex) => candidateIndex !== index && candidate.code === newCode)) {
                        window.alert('Adım kodları benzersiz olmalıdır.');
                        codeInput.value = step.code;
                        return;
                    }

                    const oldCode = step.code;
                    step.code = newCode;
                    state.flows.forEach((flow) => {
                        if (flow.startCode === oldCode) {
                            flow.startCode = newCode;
                        }
                    });
                    state.steps.forEach((other) => {
                        if (other === step) {
                            return;
                        }
                        if (other.type === STEP_TYPES.NORMAL && other.nextCode === oldCode) {
                            other.nextCode = newCode;
                        }
                        if (other.type === STEP_TYPES.DECISION) {
                            other.branches.forEach((branch) => {
                                if (branch.targetCode === oldCode) {
                                    branch.targetCode = newCode;
                                }
                            });
                        }
                    });
                    render();
                });
                header.appendChild(codeInput);

                if (!readOnly) {
                    const moveGroup = document.createElement('div');
                    moveGroup.className = 'algoritma-step-hareket';

                    const upButton = document.createElement('button');
                    upButton.type = 'button';
                    upButton.className = 'buton-link';
                    upButton.textContent = 'Yukarı';
                    upButton.disabled = index === 0;
                    upButton.addEventListener('click', () => {
                        if (index === 0) {
                            return;
                        }
                        const [removed] = state.steps.splice(index, 1);
                        state.steps.splice(index - 1, 0, removed);
                        render();
                    });
                    moveGroup.appendChild(upButton);

                    const downButton = document.createElement('button');
                    downButton.type = 'button';
                    downButton.className = 'buton-link';
                    downButton.textContent = 'Aşağı';
                    downButton.disabled = index === state.steps.length - 1;
                    downButton.addEventListener('click', () => {
                        if (index === state.steps.length - 1) {
                            return;
                        }
                        const [removed] = state.steps.splice(index, 1);
                        state.steps.splice(index + 1, 0, removed);
                        render();
                    });
                    moveGroup.appendChild(downButton);

                    header.appendChild(moveGroup);
                }

                const titleInput = document.createElement('input');
                titleInput.type = 'text';
                titleInput.placeholder = 'Adım başlığı';
                titleInput.value = step.title;
                titleInput.disabled = readOnly;
                titleInput.addEventListener('input', () => {
                    step.title = titleInput.value;
                    updateOutputs();
                });
                card.appendChild(titleInput);

                const roleInput = document.createElement('input');
                roleInput.type = 'text';
                roleInput.placeholder = 'Rol (isteğe bağlı)';
                roleInput.value = step.role;
                roleInput.disabled = readOnly;
                roleInput.addEventListener('input', () => {
                    step.role = roleInput.value;
                    updateOutputs();
                });
                card.appendChild(roleInput);

                const descriptionArea = document.createElement('textarea');
                descriptionArea.rows = 3;
                descriptionArea.placeholder = 'Açıklama (isteğe bağlı)';
                descriptionArea.value = step.description;
                descriptionArea.disabled = readOnly;
                descriptionArea.addEventListener('input', () => {
                    step.description = descriptionArea.value;
                    updateOutputs();
                });
                card.appendChild(descriptionArea);

                const typeSelect = document.createElement('select');
                typeSelect.disabled = readOnly;
                [STEP_TYPES.NORMAL, STEP_TYPES.DECISION].forEach((value) => {
                    const option = document.createElement('option');
                    option.value = value;
                    option.textContent = value === STEP_TYPES.DECISION ? 'Karar' : 'Normal';
                    typeSelect.appendChild(option);
                });
                typeSelect.value = step.type;
                typeSelect.addEventListener('change', () => {
                    if (readOnly) {
                        return;
                    }
                    step.type = typeSelect.value === STEP_TYPES.DECISION ? STEP_TYPES.DECISION : STEP_TYPES.NORMAL;
                    if (step.type === STEP_TYPES.DECISION) {
                        step.nextCode = '';
                        if (step.branches.length === 0) {
                            const defaultTarget = state.steps[index + 1]?.code ?? state.steps[0]?.code ?? '';
                            step.branches = [
                                { label: 'Evet', targetCode: defaultTarget },
                                { label: 'Hayır', targetCode: '' }
                            ];
                        }
                    }
                    else {
                        step.nextCode = step.nextCode || '';
                        step.branches = [];
                    }
                    render();
                });
                card.appendChild(typeSelect);

                if (step.type === STEP_TYPES.NORMAL) {
                    const nextSelect = document.createElement('select');
                    nextSelect.disabled = readOnly || state.steps.length <= 1;
                    const noneOption = document.createElement('option');
                    noneOption.value = '';
                    noneOption.textContent = 'Sonraki adım yok';
                    nextSelect.appendChild(noneOption);
                    state.steps.forEach((candidate) => {
                        if (candidate === step) {
                            return;
                        }
                        const option = document.createElement('option');
                        option.value = candidate.code;
                        option.textContent = `${candidate.code} - ${candidate.title || 'İsimsiz adım'}`;
                        nextSelect.appendChild(option);
                    });
                    nextSelect.value = step.nextCode || '';
                    nextSelect.addEventListener('change', () => {
                        step.nextCode = nextSelect.value;
                        updateOutputs();
                    });
                    card.appendChild(nextSelect);
                }
                else {
                    const branchContainer = document.createElement('div');
                    branchContainer.className = 'algoritma-branch-container';
                    card.appendChild(branchContainer);
                    renderBranchControls(step, branchContainer);

                    if (!readOnly) {
                        const addBranchButton = document.createElement('button');
                        addBranchButton.type = 'button';
                        addBranchButton.className = 'buton-link';
                        addBranchButton.textContent = 'Seçenek Ekle';
                        addBranchButton.addEventListener('click', () => {
                            step.branches.push({ label: '', targetCode: '' });
                            render();
                        });
                        card.appendChild(addBranchButton);
                    }
                }

                if (!readOnly) {
                    const removeButton = document.createElement('button');
                    removeButton.type = 'button';
                    removeButton.className = 'buton-link algoritma-step-sil';
                    removeButton.textContent = 'Adımı Sil';
                    removeButton.addEventListener('click', () => {
                        const removedCode = step.code;
                        state.steps.splice(index, 1);
                        state.flows = state.flows.filter((flow) => flow.startCode !== removedCode);
                        state.steps.forEach((other) => {
                            if (other.type === STEP_TYPES.NORMAL && other.nextCode === removedCode) {
                                other.nextCode = '';
                            }
                            if (other.type === STEP_TYPES.DECISION) {
                                other.branches = other.branches.filter((branch) => branch.targetCode !== removedCode);
                            }
                        });
                        render();
                    });
                    card.appendChild(removeButton);
                }

                stepList.appendChild(card);
            });
        }

        function render() {
            const codes = new Set(state.steps.map((step) => step.code));
            state.flows = state.flows.filter((flow) => codes.has(flow.startCode));
            if (state.flows.length === 0 && state.steps.length > 0) {
                state.flows.push({ name: 'Ana Akış', startCode: state.steps[0].code });
            }

            if (addFlowButton) {
                addFlowButton.disabled = readOnly || state.steps.length === 0;
            }
            if (addStepButton) {
                addStepButton.disabled = readOnly;
            }

            renderFlows();
            renderSteps();
            updateOutputs();
        }

        if (addFlowButton && !readOnly) {
            addFlowButton.addEventListener('click', () => {
                if (state.steps.length === 0) {
                    window.alert('Önce en az bir adım ekleyin.');
                    return;
                }
                state.flows.push({
                    name: `Akış ${state.flows.length + 1}`,
                    startCode: state.steps[0].code
                });
                render();
            });
        }

        if (addStepButton && !readOnly) {
            addStepButton.addEventListener('click', () => {
                const code = generateStepCode(state.steps);
                state.steps.push({
                    code,
                    title: '',
                    type: STEP_TYPES.NORMAL,
                    description: '',
                    role: '',
                    nextCode: '',
                    branches: []
                });
                render();
            });
        }

        form.addEventListener('submit', (event) => {
            const hasEmptyTitle = state.steps.some((step) => !(step.title && step.title.trim().length > 0));
            if (hasEmptyTitle) {
                event.preventDefault();
                window.alert('Lütfen tüm adımlar için başlık girin.');
                return;
            }

            const codes = new Set(state.steps.map((step) => step.code));
            const invalidFlow = state.flows.some((flow) => !(flow.name && flow.name.trim().length > 0) || !codes.has(flow.startCode));
            if (invalidFlow) {
                event.preventDefault();
                window.alert('Her akış için isim ve geçerli bir başlangıç adımı seçin.');
                return;
            }

            const invalidBranch = state.steps.some((step) => step.type === STEP_TYPES.DECISION && (
                step.branches.length === 0 ||
                step.branches.some((branch) => !(branch.label && branch.label.trim().length > 0 && branch.targetCode && codes.has(branch.targetCode)))
            ));
            if (invalidBranch) {
                event.preventDefault();
                window.alert('Karar adımları için tüm seçeneklerin etiket ve hedef adımları olmalıdır.');
                return;
            }

            updateOutputs();
        });

        render();
    }
    document.addEventListener('DOMContentLoaded', () => {
        document.querySelectorAll('[data-algorithm-display]').forEach((container) => initDisplay(container));
        document.querySelectorAll('[data-algorithm-form]').forEach((form) => initEditor(form));
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
