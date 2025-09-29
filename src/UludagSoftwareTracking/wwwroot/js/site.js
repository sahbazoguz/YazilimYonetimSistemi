(function () {
    const STEP_TYPE_NORMAL = 'Normal';
    const STEP_TYPE_DECISION = 'Decision';
    let mermaidInitialized = false;

    function getCsrfToken() {
        const meta = document.querySelector('meta[name="csrf-token"]');
        return meta ? meta.getAttribute('content') || '' : '';
    }

    function normalizeStepType(type) {
        if (typeof type === 'string' && type.toLowerCase() === STEP_TYPE_DECISION.toLowerCase()) {
            return STEP_TYPE_DECISION;
        }

        return STEP_TYPE_NORMAL;
    }

    function sanitizeString(value) {
        if (typeof value !== 'string') {
            return '';
        }

        return value.trim();
    }

    function sanitizeOptionalString(value) {
        const text = sanitizeString(value);
        return text.length > 0 ? text : '';
    }

    function cloneValue(value) {
        if (value === undefined || value === null) {
            return null;
        }

        try {
            return JSON.parse(JSON.stringify(value));
        } catch (error) {
            console.warn('Algoritma verisi kopyalanamadı:', error);
            return null;
        }
    }

    function normalizeNext(raw) {
        if (raw === undefined || raw === null) {
            return null;
        }

        if (typeof raw === 'string') {
            const text = raw.trim();
            return text.length > 0 ? text : null;
        }

        if (Array.isArray(raw)) {
            const values = Array.from(new Set(raw
                .map((item) => (typeof item === 'string' ? item.trim() : ''))
                .filter((item) => item.length > 0)));

            return values.length > 0 ? values : null;
        }

        if (typeof raw === 'object') {
            const result = {};
            Object.entries(raw).forEach(([key, value]) => {
                result[key] = normalizeNext(value);
            });
            return result;
        }

        return null;
    }

    function normalizeStep(step, index) {
        const code = typeof step?.Code === 'string'
            ? step.Code.trim()
            : (typeof step?.code === 'string' ? step.code.trim() : '');

        const title = sanitizeString(step?.Title ?? step?.title ?? '');
        const type = normalizeStepType(step?.Type ?? step?.type);
        const description = sanitizeOptionalString(step?.Description ?? step?.description ?? '');
        const role = sanitizeOptionalString(step?.Role ?? step?.role ?? '');
        const next = normalizeNext(step?.Next ?? step?.next ?? null);

        return {
            Code: code.length > 0 ? code : `A${index + 1}`,
            Title: title,
            Type: type,
            Description: description,
            Role: role,
            Next: next
        };
    }

    function ensureUniqueCodes(steps) {
        const used = new Map();
        const replacements = new Map();

        steps.forEach((step, index) => {
            const original = step.Code;
            const base = step.Code && step.Code.length > 0 ? step.Code : `A${index + 1}`;
            let candidate = base;
            let suffix = 1;

            while (used.has(candidate.toLowerCase())) {
                candidate = `${base}_${suffix}`;
                suffix++;
            }

            if (candidate !== original) {
                replacements.set(original, candidate);
            }

            step.Code = candidate;
            used.set(candidate.toLowerCase(), true);
        });

        return replacements;
    }

    function replaceCodeInNext(next, oldCode, newCode) {
        if (!next || !oldCode) {
            return next;
        }

        const target = oldCode.toLowerCase();
        const replacement = typeof newCode === 'string' && newCode.length > 0 ? newCode : null;

        if (typeof next === 'string') {
            return next.trim().toLowerCase() === target ? replacement : next;
        }

        if (Array.isArray(next)) {
            const updated = next
                .map((item) => (item && item.trim().toLowerCase() === target ? replacement : item))
                .filter((item) => typeof item === 'string' && item.length > 0);

            return updated.length > 0 ? updated : null;
        }

        if (typeof next === 'object') {
            const result = {};
            Object.entries(next).forEach(([key, value]) => {
                const replaced = replaceCodeInNext(value, oldCode, newCode);
                result[key] = replaced;
            });
            return result;
        }

        return next;
    }

    function applyCodeReplacements(steps, flows, replacements) {
        replacements.forEach((newCode, oldCode) => {
            if (!oldCode || !newCode) {
                return;
            }

            steps.forEach((step) => {
                step.Next = replaceCodeInNext(step.Next, oldCode, newCode);
            });

            flows.forEach((flow) => {
                if (flow.StartCode && flow.StartCode.toLowerCase() === oldCode.toLowerCase()) {
                    flow.StartCode = newCode;
                }
            });
        });
    }

    function ensureFlowDefaults(flows, steps) {
        const sanitized = [];
        const seen = new Set();

        flows.forEach((flow) => {
            if (!flow) {
                return;
            }

            const startCode = typeof flow.StartCode === 'string'
                ? flow.StartCode.trim()
                : (typeof flow.startCode === 'string' ? flow.startCode.trim() : '');

            if (!startCode) {
                return;
            }

            const match = steps.find((step) => step.Code.toLowerCase() === startCode.toLowerCase());
            if (!match || seen.has(match.Code.toLowerCase())) {
                return;
            }

            const label = typeof flow.Label === 'string'
                ? flow.Label.trim()
                : (typeof flow.label === 'string' ? flow.label.trim() : '');

            sanitized.push({
                Label: label.length > 0 ? label : `Akış ${sanitized.length + 1}`,
                StartCode: match.Code
            });
            seen.add(match.Code.toLowerCase());
        });

        if (sanitized.length === 0 && steps.length > 0) {
            sanitized.push({
                Label: 'Ana Akış',
                StartCode: steps[0].Code
            });
        }

        return sanitized;
    }

    function ensureStepTransitions(steps) {
        steps.forEach((step, index) => {
            if (step.Type === STEP_TYPE_DECISION) {
                if (!step.Next || Array.isArray(step.Next) || typeof step.Next !== 'object') {
                    step.Next = {};
                }

                if (!Object.prototype.hasOwnProperty.call(step.Next, 'Evet')) {
                    step.Next.Evet = index < steps.length - 1 ? steps[index + 1].Code : null;
                }

                if (!Object.prototype.hasOwnProperty.call(step.Next, 'Hayır')) {
                    step.Next['Hayır'] = null;
                }
            }
            else {
                if (!step.Next || (Array.isArray(step.Next) && step.Next.length === 0)) {
                    step.Next = index < steps.length - 1 ? steps[index + 1].Code : null;
                }
            }
        });
    }

    function sanitizeDesigner(raw) {
        if (raw === null || raw === undefined) {
            return { flows: [], steps: [] };
        }

        const candidate = Array.isArray(raw)
            ? { steps: raw, flows: [] }
            : raw;

        const rawSteps = Array.isArray(candidate?.steps)
            ? candidate.steps
            : (Array.isArray(candidate?.Steps) ? candidate.Steps : []);

        const rawFlows = Array.isArray(candidate?.flows)
            ? candidate.flows
            : (Array.isArray(candidate?.Flows) ? candidate.Flows : []);

        const steps = rawSteps.map((step, index) => normalizeStep(step, index))
            .filter((step) => step.Title.length > 0);

        const replacements = ensureUniqueCodes(steps);
        let flows = ensureFlowDefaults(rawFlows, steps);
        if (replacements.size > 0) {
            applyCodeReplacements(steps, flows, replacements);
            flows = ensureFlowDefaults(flows, steps);
        }

        ensureStepTransitions(steps);

        return { flows, steps };
    }

    function parseDesigner(json) {
        if (!json) {
            return { flows: [], steps: [] };
        }

        try {
            const parsed = JSON.parse(json);
            return sanitizeDesigner(parsed);
        }
        catch (error) {
            console.warn('Algoritma JSON verisi okunamadı:', error);
            return { flows: [], steps: [] };
        }
    }

    function targetsToArray(next) {
        if (!next) {
            return [];
        }

        if (typeof next === 'string') {
            return [next];
        }

        if (Array.isArray(next)) {
            return next;
        }

        return [];
    }

    function decisionBranches(step) {
        const branches = [];
        const next = step.Next && typeof step.Next === 'object' && !Array.isArray(step.Next)
            ? step.Next
            : {};

        const keys = new Set(Object.keys(next));
        if (!keys.has('Evet')) {
            next.Evet = null;
            keys.add('Evet');
        }
        if (!keys.has('Hayır')) {
            next['Hayır'] = null;
            keys.add('Hayır');
        }

        keys.forEach((key) => {
            branches.push({
                key,
                value: next[key]
            });
        });

        return branches;
    }

    function setDecisionBranch(step, key, value) {
        if (!step.Next || typeof step.Next !== 'object' || Array.isArray(step.Next)) {
            step.Next = {};
        }

        step.Next[key] = value;
    }

    function removeDecisionBranch(step, key) {
        if (!step.Next || typeof step.Next !== 'object' || Array.isArray(step.Next)) {
            return;
        }

        if (key === 'Evet' || key === 'Hayır') {
            step.Next[key] = null;
            return;
        }

        delete step.Next[key];
    }

    function renderFlowPreview(container, designer) {
        if (!container) {
            return;
        }

        container.innerHTML = '';

        if (!Array.isArray(designer.steps) || designer.steps.length === 0) {
            const empty = document.createElement('p');
            empty.className = 'yardim-metin';
            empty.textContent = 'Henüz adım eklenmedi.';
            container.appendChild(empty);
            return;
        }

        if (Array.isArray(designer.flows) && designer.flows.length > 0) {
            const flowList = document.createElement('ul');
            flowList.className = 'akisma-onizleme-akislar';

            designer.flows.forEach((flow) => {
                const item = document.createElement('li');
                const label = document.createElement('strong');
                label.textContent = flow.Label;
                item.appendChild(label);
                item.appendChild(document.createTextNode(`: ${flow.StartCode}`));
                flowList.appendChild(item);
            });

            container.appendChild(flowList);
        }

        designer.steps.forEach((step) => {
            const card = document.createElement('div');
            card.className = 'akisma-kart';

            const title = document.createElement('div');
            title.className = 'akisma-kart-baslik';
            const stepTitle = step.Title && step.Title.trim().length > 0 ? ` - ${step.Title.trim()}` : '';
            title.textContent = `${step.Code}${stepTitle}`;
            card.appendChild(title);

            if (step.Type === STEP_TYPE_DECISION) {
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

            if (step.Type === STEP_TYPE_DECISION && step.Next && typeof step.Next === 'object' && !Array.isArray(step.Next)) {
                const branchesWrapper = document.createElement('div');
                branchesWrapper.className = 'akisma-kart-dallar';

                Object.entries(step.Next).forEach(([label, value]) => {
                    const branchItem = document.createElement('span');
                    branchItem.className = 'akisma-kart-dal';
                    const labelStrong = document.createElement('strong');
                    labelStrong.textContent = `${label}:`;
                    branchItem.appendChild(labelStrong);
                    const targets = Array.isArray(value) ? value.join(', ') : (value ?? '—');
                    branchItem.appendChild(document.createTextNode(` ${targets}`));
                    branchesWrapper.appendChild(branchItem);
                });

                card.appendChild(branchesWrapper);
            }
            else if (step.Next) {
                const targets = targetsToArray(step.Next);
                const nextInfo = document.createElement('p');
                nextInfo.className = 'akisma-kart-gecis';
                nextInfo.textContent = targets.length > 0
                    ? `Sonraki: ${targets.join(', ')}`
                    : 'Sonraki adım yok';
                card.appendChild(nextInfo);
            }

            container.appendChild(card);
        });
    }

    function buildGraph(designer) {
        const nodes = new Map();
        const adjacency = new Map();
        const edges = [];

        designer.steps.forEach((step) => {
            nodes.set(step.Code, step);
            adjacency.set(step.Code, new Set());
        });

        designer.steps.forEach((step) => {
            if (!step.Next) {
                return;
            }

            if (typeof step.Next === 'string') {
                adjacency.get(step.Code)?.add(step.Next);
                edges.push({ from: step.Code, to: step.Next, label: null });
                return;
            }

            if (Array.isArray(step.Next)) {
                step.Next.forEach((target) => {
                    adjacency.get(step.Code)?.add(target);
                    edges.push({ from: step.Code, to: target, label: null });
                });
                return;
            }

            if (typeof step.Next === 'object') {
                Object.entries(step.Next).forEach(([label, value]) => {
                    if (!value) {
                        return;
                    }

                    if (typeof value === 'string') {
                        adjacency.get(step.Code)?.add(value);
                        edges.push({ from: step.Code, to: value, label });
                    }
                    else if (Array.isArray(value)) {
                        value.forEach((target) => {
                            adjacency.get(step.Code)?.add(target);
                            edges.push({ from: step.Code, to: target, label });
                        });
                    }
                });
            }
        });

        const starts = designer.flows
            .map((flow) => flow.StartCode)
            .filter((code) => typeof code === 'string' && nodes.has(code));

        return { nodes, adjacency, edges, starts };
    }

    function ensureMermaidInstance() {
        const instance = window.mermaid;
        if (!instance || typeof instance.initialize !== 'function') {
            return null;
        }

        if (!mermaidInitialized) {
            instance.initialize({
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

        return instance;
    }

    function escapeMermaidText(text) {
        if (typeof text !== 'string') {
            return '';
        }

        return text
            .replace(/&/g, '&amp;')
            .replace(/\\/g, '\\\\')
            .replace(/"/g, '\\"')
            .replace(/\[/g, '\\[')
            .replace(/\]/g, '\\]')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;');
    }

    function createMermaidIdMap(steps) {
        const map = new Map();
        const used = new Set();

        steps.forEach((step, index) => {
            const rawCode = typeof step.Code === 'string' && step.Code.length > 0
                ? step.Code
                : `Adim_${index + 1}`;
            const base = rawCode.replace(/[^A-Za-z0-9_]/g, '_') || `Adim_${index + 1}`;
            let candidate = base;
            let suffix = 1;

            while (used.has(candidate)) {
                candidate = `${base}_${suffix}`;
                suffix++;
            }

            used.add(candidate);
            map.set(step.Code, candidate);
        });

        return map;
    }

    function buildFlowSubgraph(graph, startCode) {
        const visited = new Set();
        const queue = [];

        if (startCode && graph.nodes.has(startCode)) {
            queue.push(startCode);
        }

        while (queue.length > 0) {
            const current = queue.shift();
            if (!current || visited.has(current) || !graph.nodes.has(current)) {
                continue;
            }

            visited.add(current);
            const neighbours = graph.adjacency.get(current) ?? new Set();
            neighbours.forEach((target) => {
                if (!visited.has(target) && graph.nodes.has(target)) {
                    queue.push(target);
                }
            });
        }

        const nodes = Array.from(visited)
            .map((code) => graph.nodes.get(code))
            .filter((step) => step && typeof step.Code === 'string');

        const edges = graph.edges.filter((edge) => visited.has(edge.from) && visited.has(edge.to));

        return { nodes, edges };
    }

    function buildMermaidDefinition(flow, graph) {
        if (!flow || typeof flow.StartCode !== 'string') {
            return null;
        }

        const { nodes, edges } = buildFlowSubgraph(graph, flow.StartCode);
        if (nodes.length === 0) {
            return null;
        }

        const idMap = createMermaidIdMap(nodes);
        const lines = ['flowchart TD'];

        nodes.forEach((step) => {
            const id = idMap.get(step.Code) ?? step.Code;
            const parts = [];
            const title = step.Title && step.Title.trim().length > 0 ? step.Title.trim() : 'Adım';
            parts.push(`${step.Code}: ${title}`);

            if (step.Role && step.Role.trim().length > 0) {
                parts.push(`Rol: ${step.Role.trim()}`);
            }

            if (step.Description && step.Description.trim().length > 0) {
                parts.push(step.Description.trim());
            }

            const label = parts.map(escapeMermaidText).join('\\n');
            const nodeDefinition = step.Type === STEP_TYPE_DECISION
                ? `${id}{"${label}"}`
                : `${id}["${label}"]`;
            lines.push(nodeDefinition);
        });

        const seenEdges = new Set();
        edges.forEach((edge) => {
            const fromId = idMap.get(edge.from) ?? edge.from;
            const toId = idMap.get(edge.to) ?? edge.to;

            if (!fromId || !toId) {
                return;
            }

            const label = edge.label && edge.label.trim().length > 0
                ? escapeMermaidText(edge.label.trim())
                : '';
            const key = `${fromId}->${toId}|${label}`;
            if (seenEdges.has(key)) {
                return;
            }
            seenEdges.add(key);

            if (label.length > 0) {
                lines.push(`${fromId} -->|${label}| ${toId}`);
            }
            else {
                lines.push(`${fromId} --> ${toId}`);
            }
        });

        return lines.join('\n');
    }

    function renderFlowDiagram(container, designer) {
        if (!container) {
            return;
        }

        container.innerHTML = '';

        if (!Array.isArray(designer.steps) || designer.steps.length === 0) {
            const empty = document.createElement('p');
            empty.className = 'yardim-metin';
            empty.textContent = 'Diyagram oluşturulacak adım bulunmuyor.';
            container.appendChild(empty);
            return;
        }

        const graph = buildGraph(designer);
        const flows = Array.isArray(designer.flows) && designer.flows.length > 0
            ? designer.flows
            : [{ Label: 'Ana Akış', StartCode: designer.steps[0].Code }];

        const mermaidApi = ensureMermaidInstance();
        const hasRenderer = !!mermaidApi && typeof mermaidApi.render === 'function';
        let rendered = false;

        flows.forEach((flow, index) => {
            if (!flow || typeof flow.StartCode !== 'string' || !graph.nodes.has(flow.StartCode)) {
                return;
            }

            const definition = buildMermaidDefinition(flow, graph);
            const wrapper = document.createElement('div');
            wrapper.className = 'akisma-diagram-parca';

            const heading = document.createElement('h5');
            heading.className = 'akisma-diagram-parca-baslik';
            heading.textContent = flow.Label && flow.Label.length > 0
                ? flow.Label
                : `Akış ${index + 1}`;
            wrapper.appendChild(heading);

            if (!definition) {
                const warning = document.createElement('p');
                warning.className = 'akisma-diagram-uyari';
                warning.textContent = 'Bu akış için görselleştirilecek adım bulunamadı.';
                wrapper.appendChild(warning);
                container.appendChild(wrapper);
                rendered = true;
                return;
            }

            if (!hasRenderer) {
                const pre = document.createElement('pre');
                pre.className = 'akisma-diagram-kod';
                pre.textContent = definition;
                wrapper.appendChild(pre);
                container.appendChild(wrapper);
                rendered = true;
                return;
            }

            const host = document.createElement('div');
            host.className = 'akisma-mermaid';
            wrapper.appendChild(host);
            container.appendChild(wrapper);

            const renderId = `mermaid-${Date.now()}-${Math.random().toString(36).slice(2)}`;
            mermaidApi.render(renderId, definition)
                .then(({ svg, bindFunctions }) => {
                    host.innerHTML = svg;
                    if (typeof bindFunctions === 'function') {
                        bindFunctions(host);
                    }
                })
                .catch((error) => {
                    console.warn('Mermaid diyagramı oluşturulamadı:', error);
                    const fallback = document.createElement('pre');
                    fallback.className = 'akisma-diagram-kod';
                    fallback.textContent = definition;
                    host.replaceWith(fallback);
                });

            rendered = true;
        });

        if (!rendered) {
            const empty = document.createElement('p');
            empty.className = 'yardim-metin';
            empty.textContent = 'Geçerli başlangıç adımı bulunamadı.';
            container.appendChild(empty);
        }
    }

    function initFlowEditor(form) {
        const hiddenInput = form.querySelector('[data-flow-input]');
        const editor = form.querySelector('[data-flow-editor]');
        const list = editor?.querySelector('[data-flow-list]');
        const addButton = editor?.querySelector('[data-flow-add]');
        const startPanel = editor?.querySelector('[data-flow-starts]');
        const startList = startPanel?.querySelector('[data-flow-start-list]');
        const startAddButton = startPanel?.querySelector('[data-flow-start-add]');

        if (!hiddenInput || !editor || !list || !addButton || !startPanel || !startList || !startAddButton) {
            return;
        }

        let designer = sanitizeDesigner(parseDesigner(hiddenInput.value));

        const previewSelector = form.getAttribute('data-flow-target');
        const previewElement = previewSelector ? document.querySelector(previewSelector) : null;

        const diagramSelector = form.getAttribute('data-flow-diagram-target');
        const diagramElement = diagramSelector ? document.querySelector(diagramSelector) : null;

        function pruneFlows() {
            const validCodes = new Set(designer.steps.map((step) => step.Code.toLowerCase()));
            designer.flows = designer.flows.filter((flow) => validCodes.has(flow.StartCode.toLowerCase()));
            if (designer.flows.length === 0 && designer.steps.length > 0) {
                designer.flows.push({
                    Label: 'Ana Akış',
                    StartCode: designer.steps[0].Code
                });
            }
        }

        function syncState() {
            pruneFlows();
            hiddenInput.value = JSON.stringify({
                flows: designer.flows.map((flow) => ({
                    label: flow.Label,
                    startCode: flow.StartCode
                })),
                steps: designer.steps.map((step) => ({
                    code: step.Code,
                    title: step.Title,
                    type: step.Type,
                    description: step.Description,
                    role: step.Role,
                    next: cloneValue(step.Next)
                }))
            });

            if (previewElement) {
                previewElement.setAttribute('data-flow-source', hiddenInput.value);
                renderFlowPreview(previewElement, designer);
            }

            if (diagramElement) {
                diagramElement.setAttribute('data-flow-source', hiddenInput.value);
                renderFlowDiagram(diagramElement, designer);
            }
        }

        function updateAllSelectOptions() {
            list.querySelectorAll('select[data-next-select]').forEach((select) => {
                const current = select.getAttribute('data-step-code');
                const selectedValues = Array.from(select.selectedOptions).map((option) => option.value);
                select.innerHTML = '';

                designer.steps.forEach((step) => {
                    const option = document.createElement('option');
                    option.value = step.Code;
                    option.textContent = `${step.Code} - ${step.Title || 'İsimsiz adım'}`;
                    if (step.Code === current) {
                        option.disabled = true;
                    }
                    select.appendChild(option);
                });

                selectedValues.forEach((value) => {
                    const match = Array.from(select.options).find((option) => option.value === value);
                    if (match) {
                        match.selected = true;
                    }
                });
            });

            startList.querySelectorAll('select').forEach((select) => {
                const current = select.value;
                select.innerHTML = '';
                designer.steps.forEach((step) => {
                    const option = document.createElement('option');
                    option.value = step.Code;
                    option.textContent = `${step.Code} - ${step.Title || 'İsimsiz adım'}`;
                    select.appendChild(option);
                });
                const selected = Array.from(select.options).find((option) => option.value === current);
                if (selected) {
                    selected.selected = true;
                }
            });
        }

        function renameReferences(oldCode, newCode) {
            designer.steps.forEach((step) => {
                if (step.Code === oldCode) {
                    return;
                }
                step.Next = replaceCodeInNext(step.Next, oldCode, newCode);
            });

            designer.flows.forEach((flow) => {
                if (flow.StartCode.toLowerCase() === oldCode.toLowerCase()) {
                    flow.StartCode = newCode;
                }
            });
        }

        function renderFlows() {
            const locked = addButton.disabled;
            startAddButton.disabled = locked;
            startList.innerHTML = '';
            pruneFlows();

            if (designer.flows.length === 0) {
                const empty = document.createElement('li');
                empty.className = 'akisma-baslangic-bos';
                empty.textContent = 'Başlangıç akışı bulunmuyor.';
                startList.appendChild(empty);
                return;
            }

            designer.flows.forEach((flow, index) => {
                const item = document.createElement('li');
                item.className = 'akisma-baslangic-kalem';

                const labelInput = document.createElement('input');
                labelInput.type = 'text';
                labelInput.value = flow.Label;
                labelInput.placeholder = 'Akış adı';
                labelInput.className = 'form-control akisma-baslangic-isim';
                labelInput.disabled = locked;
                labelInput.addEventListener('input', () => {
                    flow.Label = labelInput.value.trim();
                    syncState();
                });
                item.appendChild(labelInput);

                const startSelect = document.createElement('select');
                startSelect.className = 'form-control akisma-baslangic-secim';
                startSelect.disabled = locked;
                designer.steps.forEach((step) => {
                    const option = document.createElement('option');
                    option.value = step.Code;
                    option.textContent = `${step.Code} - ${step.Title || 'İsimsiz adım'}`;
                    startSelect.appendChild(option);
                });
                startSelect.value = flow.StartCode;
                startSelect.addEventListener('change', () => {
                    flow.StartCode = startSelect.value;
                    syncState();
                });
                item.appendChild(startSelect);

                const removeButton = document.createElement('button');
                removeButton.type = 'button';
                removeButton.className = 'buton-link akisma-baslangic-sil';
                removeButton.textContent = 'Sil';
                removeButton.disabled = locked || designer.flows.length === 1;
                removeButton.addEventListener('click', () => {
                    if (locked || designer.flows.length === 1) {
                        return;
                    }
                    if (designer.flows.length === 1) {
                        return;
                    }
                    designer.flows.splice(index, 1);
                    renderFlows();
                    syncState();
                });
                item.appendChild(removeButton);

                startList.appendChild(item);
            });
        }

        function renderList() {
            list.innerHTML = '';

            if (designer.steps.length === 0) {
                const empty = document.createElement('li');
                empty.className = 'akisma-editor-bos';
                empty.textContent = 'Henüz adım bulunmuyor. Yeni adım ekleyin.';
                list.appendChild(empty);
                return;
            }

            const locked = addButton.disabled;

            designer.steps.forEach((step, index) => {
                const item = document.createElement('li');
                item.className = 'akisma-editor-adim';
                if (locked) {
                    item.setAttribute('draggable', 'false');
                    item.classList.add('kilitli');
                }
                else {
                    item.setAttribute('draggable', 'true');
                }
                item.dataset.index = index.toString();

                const handle = document.createElement('span');
                handle.className = 'akisma-editor-tutamak';
                handle.innerHTML = '&#9776;';
                if (locked) {
                    handle.classList.add('pasif');
                }
                item.appendChild(handle);

                const codeInput = document.createElement('input');
                codeInput.type = 'text';
                codeInput.className = 'form-control akisma-editor-kod-input';
                codeInput.value = step.Code;
                codeInput.disabled = locked;
                codeInput.addEventListener('change', () => {
                    if (locked) {
                        codeInput.value = step.Code;
                        return;
                    }
                    const newCode = codeInput.value.trim();
                    if (newCode.length === 0) {
                        window.alert('Kod boş bırakılamaz.');
                        codeInput.value = step.Code;
                        return;
                    }

                    const conflict = designer.steps.some((other, otherIndex) => otherIndex !== index && other.Code.toLowerCase() === newCode.toLowerCase());
                    if (conflict) {
                        window.alert('Bu kod başka bir adım tarafından kullanılıyor.');
                        codeInput.value = step.Code;
                        return;
                    }

                    const oldCode = step.Code;
                    step.Code = newCode;
                    renameReferences(oldCode, newCode);
                    renderFlows();
                    updateAllSelectOptions();
                    syncState();
                });
                item.appendChild(codeInput);

                const fields = document.createElement('div');
                fields.className = 'akisma-editor-icerik';

                const typeSelect = document.createElement('select');
                typeSelect.className = 'form-control akisma-editor-tur';
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
                typeSelect.disabled = locked;
                typeSelect.addEventListener('change', () => {
                    if (locked) {
                        typeSelect.value = step.Type;
                        return;
                    }
                    step.Type = typeSelect.value;
                    if (step.Type === STEP_TYPE_DECISION) {
                        if (!step.Next || typeof step.Next !== 'object' || Array.isArray(step.Next)) {
                            step.Next = { Evet: index < designer.steps.length - 1 ? designer.steps[index + 1].Code : null, 'Hayır': null };
                        }
                    }
                    else {
                        const defaultNext = index < designer.steps.length - 1 ? designer.steps[index + 1].Code : null;
                        if (!step.Next || typeof step.Next === 'object') {
                            step.Next = defaultNext;
                        }
                    }
                    renderList();
                    syncState();
                });
                fields.appendChild(typeSelect);

                const titleInput = document.createElement('input');
                titleInput.type = 'text';
                titleInput.className = 'form-control akisma-editor-baslik';
                titleInput.placeholder = 'Adım başlığı';
                titleInput.value = step.Title || '';
                titleInput.disabled = locked;
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
                descriptionInput.disabled = locked;
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
                roleInput.disabled = locked;
                roleInput.addEventListener('input', () => {
                    step.Role = roleInput.value;
                    syncState();
                });
                fields.appendChild(roleInput);

                const nextContainer = document.createElement('div');
                nextContainer.className = 'akisma-editor-gecis';

                if (step.Type === STEP_TYPE_DECISION) {
                    const branches = decisionBranches(step);
                    branches.forEach((branch) => {
                        const branchRow = document.createElement('div');
                        branchRow.className = 'akisma-editor-dal';

                        const branchLabel = document.createElement('input');
                        branchLabel.type = 'text';
                        branchLabel.className = 'form-control akisma-editor-dal-etiket';
                        branchLabel.value = branch.key;
                        branchLabel.disabled = locked || branch.key === 'Evet' || branch.key === 'Hayır';
                        branchLabel.addEventListener('change', () => {
                            if (locked) {
                                branchLabel.value = branch.key;
                                return;
                            }
                            const newLabel = branchLabel.value.trim();
                            if (newLabel.length === 0) {
                                window.alert('Dal etiketi boş olamaz.');
                                branchLabel.value = branch.key;
                                return;
                            }
                            if (newLabel !== branch.key) {
                                const currentValue = step.Next?.[branch.key];
                                removeDecisionBranch(step, branch.key);
                                setDecisionBranch(step, newLabel, currentValue ?? null);
                                renderList();
                                syncState();
                            }
                        });
                        branchRow.appendChild(branchLabel);

                        const branchSelect = document.createElement('select');
                        branchSelect.className = 'form-control akisma-editor-dal-secim';
                        branchSelect.multiple = true;
                        branchSelect.dataset.nextSelect = 'true';
                        branchSelect.setAttribute('data-step-code', step.Code);
                        branchSelect.disabled = locked;
                        designer.steps.forEach((candidate) => {
                            const option = document.createElement('option');
                            option.value = candidate.Code;
                            option.textContent = `${candidate.Code} - ${candidate.Title || 'İsimsiz adım'}`;
                            if (candidate.Code === step.Code) {
                                option.disabled = true;
                            }
                            branchSelect.appendChild(option);
                        });
                        const branchTargets = targetsToArray(branch.value);
                        branchSelect.querySelectorAll('option').forEach((option) => {
                            if (branchTargets.includes(option.value)) {
                                option.selected = true;
                            }
                        });
                        branchSelect.addEventListener('change', () => {
                            if (locked) {
                                return;
                            }
                            const selected = Array.from(branchSelect.selectedOptions).map((option) => option.value);
                            if (selected.length === 0) {
                                setDecisionBranch(step, branchLabel.value, null);
                            }
                            else if (selected.length === 1) {
                                setDecisionBranch(step, branchLabel.value, selected[0]);
                            }
                            else {
                                setDecisionBranch(step, branchLabel.value, selected);
                            }
                            syncState();
                        });
                        branchRow.appendChild(branchSelect);

                        if (branch.key !== 'Evet' && branch.key !== 'Hayır') {
                            const removeBranch = document.createElement('button');
                            removeBranch.type = 'button';
                            removeBranch.className = 'buton-link akisma-editor-dal-sil';
                            removeBranch.textContent = 'Dal Sil';
                            removeBranch.disabled = locked;
                            removeBranch.addEventListener('click', () => {
                                if (locked) {
                                    return;
                                }
                                removeDecisionBranch(step, branch.key);
                                renderList();
                                syncState();
                            });
                            branchRow.appendChild(removeBranch);
                        }

                        nextContainer.appendChild(branchRow);
                    });

                    const addBranchButton = document.createElement('button');
                    addBranchButton.type = 'button';
                    addBranchButton.className = 'buton-link akisma-editor-dal-ekle';
                    addBranchButton.textContent = 'Yeni Dal Ekle';
                    addBranchButton.disabled = locked;
                    addBranchButton.addEventListener('click', () => {
                        if (locked) {
                            return;
                        }
                        let counter = 1;
                        let label = `Dal ${counter}`;
                        while (step.Next && typeof step.Next === 'object' && Object.prototype.hasOwnProperty.call(step.Next, label)) {
                            counter++;
                            label = `Dal ${counter}`;
                        }
                        setDecisionBranch(step, label, null);
                        renderList();
                        syncState();
                    });
                    nextContainer.appendChild(addBranchButton);
                }
                else {
                    const nextLabel = document.createElement('label');
                    nextLabel.textContent = 'Sonraki Adımlar';
                    nextLabel.className = 'akisma-editor-gecis-etiket';
                    nextContainer.appendChild(nextLabel);

                    const nextSelect = document.createElement('select');
                    nextSelect.className = 'form-control akisma-editor-gecis-secim';
                    nextSelect.multiple = true;
                    nextSelect.dataset.nextSelect = 'true';
                    nextSelect.setAttribute('data-step-code', step.Code);
                    nextSelect.disabled = locked;

                    designer.steps.forEach((candidate) => {
                        const option = document.createElement('option');
                        option.value = candidate.Code;
                        option.textContent = `${candidate.Code} - ${candidate.Title || 'İsimsiz adım'}`;
                        if (candidate.Code === step.Code) {
                            option.disabled = true;
                        }
                        nextSelect.appendChild(option);
                    });

                    const selectedTargets = targetsToArray(step.Next);
                    nextSelect.querySelectorAll('option').forEach((option) => {
                        if (selectedTargets.includes(option.value)) {
                            option.selected = true;
                        }
                    });

                    nextSelect.addEventListener('change', () => {
                        if (locked) {
                            return;
                        }
                        const selected = Array.from(nextSelect.selectedOptions).map((option) => option.value);
                        if (selected.length === 0) {
                            step.Next = null;
                        }
                        else if (selected.length === 1) {
                            step.Next = selected[0];
                        }
                        else {
                            step.Next = selected;
                        }
                        syncState();
                    });

                    nextContainer.appendChild(nextSelect);
                }

                fields.appendChild(nextContainer);
                item.appendChild(fields);

                const removeButton = document.createElement('button');
                removeButton.type = 'button';
                removeButton.className = 'buton-link akisma-editor-sil';
                removeButton.textContent = 'Sil';
                removeButton.disabled = locked;
                removeButton.addEventListener('click', () => {
                    if (locked) {
                        return;
                    }
                    const removedCode = step.Code;
                    designer.steps.splice(index, 1);
                    designer.steps.forEach((otherStep) => {
                        otherStep.Next = replaceCodeInNext(otherStep.Next, removedCode, null);
                        if (Array.isArray(otherStep.Next)) {
                            otherStep.Next = otherStep.Next.filter((target) => target !== null && target !== undefined && target !== '');
                            if (otherStep.Next.length === 0) {
                                otherStep.Next = null;
                            }
                        }
                    });
                    designer.flows = designer.flows.filter((flow) => flow.StartCode !== removedCode);
                    renderList();
                    renderFlows();
                    updateAllSelectOptions();
                    syncState();
                });
                item.appendChild(removeButton);

                if (!locked) {
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
                            const [moved] = designer.steps.splice(fromIndex, 1);
                            designer.steps.splice(toIndex, 0, moved);
                            renderList();
                            renderFlows();
                            updateAllSelectOptions();
                            syncState();
                        }
                    });
                }

                list.appendChild(item);
            });
        }

        addButton.addEventListener('click', () => {
            if (addButton.disabled) {
                return;
            }
            designer.steps.push({
                Code: `A${designer.steps.length + 1}`,
                Title: '',
                Description: '',
                Role: '',
                Type: STEP_TYPE_NORMAL,
                Next: null
            });
            renderList();
            renderFlows();
            updateAllSelectOptions();
            syncState();
        });

        startAddButton.addEventListener('click', () => {
            if (startAddButton.disabled) {
                return;
            }

            if (designer.steps.length === 0) {
                window.alert('Önce en az bir adım ekleyin.');
                return;
            }

            designer.flows.push({
                Label: `Akış ${designer.flows.length + 1}`,
                StartCode: designer.steps[0].Code
            });
            renderFlows();
            syncState();
        });

        form.addEventListener('submit', (event) => {
            const hasEmptyTitle = designer.steps.length > 0 && designer.steps.some((step) => !(step.Title && step.Title.trim().length > 0));
            if (hasEmptyTitle) {
                event.preventDefault();
                window.alert('Lütfen tüm algoritma adımları için başlık girin.');
                return;
            }

            syncState();
        });

        renderList();
        renderFlows();
        updateAllSelectOptions();
        syncState();
    }

    document.addEventListener('DOMContentLoaded', () => {
        document.querySelectorAll('[data-flow-preview]').forEach((container) => {
            const source = container.getAttribute('data-flow-source') || '';
            const designer = parseDesigner(source);
            renderFlowPreview(container, designer);
        });

        document.querySelectorAll('[data-flow-diagram]').forEach((container) => {
            const source = container.getAttribute('data-flow-source') || '';
            const designer = parseDesigner(source);
            renderFlowDiagram(container, designer);
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
