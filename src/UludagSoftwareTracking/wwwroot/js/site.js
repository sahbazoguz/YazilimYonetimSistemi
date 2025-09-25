(function () {
    function getCsrfToken() {
        const meta = document.querySelector('meta[name="csrf-token"]');
        return meta ? meta.getAttribute('content') || '' : '';
    }

    function parseSteps(json) {
        if (!json) {
            return [];
        }

        try {
            const parsed = JSON.parse(json);
            if (Array.isArray(parsed)) {
                return parsed.map((step) => ({
                    Title: typeof step?.Title === 'string' ? step.Title : (typeof step?.title === 'string' ? step.title : ''),
                    Description: typeof step?.Description === 'string' ? step.Description : (typeof step?.description === 'string' ? step.description : '')
                }));
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
            title.textContent = step.Title || `Adım ${index + 1}`;
            card.appendChild(title);

            if (step.Description) {
                const desc = document.createElement('p');
                desc.className = 'akisma-kart-icerik';
                desc.textContent = step.Description;
                card.appendChild(desc);
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

        function syncState() {
            const serialized = steps.map((step) => ({
                Title: (step.Title || '').trim(),
                Description: (step.Description || '').trim()
            }));
            hiddenInput.value = JSON.stringify(serialized);
            if (previewElement) {
                previewElement.setAttribute('data-flow-source', hiddenInput.value);
                renderFlowPreview(previewElement, serialized);
            }
        }

        function renderList() {
            list.innerHTML = '';

            if (steps.length === 0) {
                const empty = document.createElement('li');
                empty.className = 'akisma-editor-bos';
                empty.textContent = 'Henüz adım bulunmuyor. Yeni adım ekleyin.';
                list.appendChild(empty);
                return;
            }

            steps.forEach((step, index) => {
                const item = document.createElement('li');
                item.className = 'akisma-editor-adim';
                item.setAttribute('draggable', 'true');
                item.dataset.index = index.toString();

                const handle = document.createElement('span');
                handle.className = 'akisma-editor-tutamak';
                handle.innerHTML = '&#9776;';
                item.appendChild(handle);

                const fields = document.createElement('div');
                fields.className = 'akisma-editor-icerik';

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
            steps.push({ Title: `Adım ${steps.length + 1}`, Description: '' });
            renderList();
            syncState();
        });

        form.addEventListener('submit', () => {
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
