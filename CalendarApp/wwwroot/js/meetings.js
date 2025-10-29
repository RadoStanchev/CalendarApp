(function () {
    const STATUS_OPTIONS = [
        { value: '0', label: 'Pending' },
        { value: '1', label: 'Accepted' },
        { value: '2', label: 'Declined' }
    ];

    function debounce(fn, delay) {
        let timer;
        return (...args) => {
            clearTimeout(timer);
            timer = setTimeout(() => fn.apply(null, args), delay);
        };
    }

    function createEmptyNotice(container) {
        const text = container.dataset.emptyText || 'Add participants to this meeting.';
        const notice = document.createElement('p');
        notice.className = 'text-muted small mb-0';
        notice.dataset.meetingEmpty = '';
        notice.textContent = text;
        container.prepend(notice);
        return notice;
    }

    function updateEmptyState(container) {
        const notice = container.querySelector('[data-meeting-empty]') || createEmptyNotice(container);
        const hasParticipants = container.querySelectorAll('[data-participant]').length > 0;
        if (hasParticipants) {
            notice.classList.add('d-none');
        } else {
            notice.classList.remove('d-none');
        }
    }

    function updateFieldNames(container) {
        const items = container.querySelectorAll('[data-participant]');
        items.forEach((item, index) => {
            const contactInput = item.querySelector('[data-contact-input]');
            if (contactInput) {
                contactInput.name = `Participants[${index}].ContactId`;
            }

            const statusInput = item.querySelector('[data-status-input]');
            if (statusInput) {
                statusInput.name = `Participants[${index}].Status`;
            }

            const statusSelect = item.querySelector('[data-status-select]');
            if (statusSelect) {
                statusSelect.name = `Participants[${index}].Status`;
            }
        });
    }

    function buildParticipantElement(showStatus, suggestion) {
        const wrapper = document.createElement('div');
        wrapper.dataset.participant = '';
        wrapper.dataset.contactId = suggestion.id;
        wrapper.dataset.isCreator = 'false';

        if (showStatus) {
            wrapper.className = 'border rounded p-3 mb-2';
            wrapper.innerHTML = `
                <div class="d-flex flex-column flex-md-row align-items-md-center justify-content-between gap-3">
                    <div>
                        <div class="fw-semibold">${suggestion.displayName}</div>
                        <div class="text-muted small">${suggestion.email}</div>
                    </div>
                    <div class="d-flex align-items-center gap-2">
                        <select class="form-select form-select-sm" data-status-select>
                            ${STATUS_OPTIONS.map(option => `<option value="${option.value}"${option.value === '0' ? ' selected' : ''}>${option.label}</option>`).join('')}
                        </select>
                        <button type="button" class="btn btn-sm btn-outline-danger" data-remove-participant>&times;</button>
                    </div>
                </div>
                <input type="hidden" data-contact-input value="${suggestion.id}" />
            `;
        } else {
            wrapper.className = 'border rounded p-3 mb-2 d-flex justify-content-between align-items-start gap-3';
            wrapper.innerHTML = `
                <div>
                    <div class="fw-semibold">${suggestion.displayName}</div>
                    <div class="text-muted small">${suggestion.email}</div>
                </div>
                <div class="text-end">
                    <button type="button" class="btn btn-sm btn-outline-danger" data-remove-participant>&times;</button>
                </div>
                <input type="hidden" data-contact-input value="${suggestion.id}" />
                <input type="hidden" data-status-input value="0" />
            `;
        }

        return wrapper;
    }

    function init(config) {
        const root = document.querySelector(config?.rootSelector ?? '[data-meeting-search-root]');
        if (!root) {
            return;
        }

        const showStatus = config?.showStatus ?? (root.dataset.showStatus === 'true');
        const searchUrl = root.dataset.searchUrl;
        const searchInput = root.querySelector('[data-meeting-search]');
        const resultsContainer = root.querySelector('[data-meeting-search-results]');
        const selectedContainer = root.querySelector('[data-meeting-selected]');
        if (!searchUrl || !searchInput || !resultsContainer || !selectedContainer) {
            return;
        }

        const participants = new Map();
        selectedContainer.querySelectorAll('[data-participant]').forEach(item => {
            const contactId = item.dataset.contactId;
            if (contactId) {
                participants.set(contactId, item);
            }
        });
        updateEmptyState(selectedContainer);
        updateFieldNames(selectedContainer);

        function hideResults() {
            resultsContainer.classList.add('d-none');
            resultsContainer.innerHTML = '';
        }

        function addParticipant(suggestion) {
            if (!suggestion || participants.has(suggestion.id)) {
                hideResults();
                return;
            }

            const element = buildParticipantElement(showStatus, suggestion);
            selectedContainer.appendChild(element);
            participants.set(suggestion.id, element);
            updateEmptyState(selectedContainer);
            updateFieldNames(selectedContainer);
            hideResults();
        }

        selectedContainer.addEventListener('click', (event) => {
            const target = event.target;
            if (!(target instanceof HTMLElement)) {
                return;
            }

            if (target.matches('[data-remove-participant]')) {
                const participant = target.closest('[data-participant]');
                if (!participant) {
                    return;
                }

                if (participant.dataset.isCreator === 'true') {
                    return;
                }

                const contactId = participant.dataset.contactId;
                participant.remove();
                if (contactId) {
                    participants.delete(contactId);
                }

                updateEmptyState(selectedContainer);
                updateFieldNames(selectedContainer);
            }
        });

        let queryToken = 0;
        const performSearch = debounce(async () => {
            const term = searchInput.value.trim();
            if (term.length < 2) {
                hideResults();
                return;
            }

            const token = ++queryToken;
            const exclude = Array.from(participants.keys());
            const url = new URL(searchUrl, window.location.origin);
            url.searchParams.set('term', term);
            if (exclude.length > 0) {
                url.searchParams.set('exclude', exclude.join(','));
            }

            try {
                const response = await fetch(url.toString(), {
                    headers: { 'Accept': 'application/json' },
                    credentials: 'same-origin'
                });

                if (!response.ok || token !== queryToken) {
                    return;
                }

                const suggestions = await response.json();
                if (!Array.isArray(suggestions) || suggestions.length === 0) {
                    resultsContainer.innerHTML = '<div class="list-group-item text-muted">No matches found.</div>';
                    resultsContainer.classList.remove('d-none');
                    return;
                }

                resultsContainer.innerHTML = '';
                suggestions.forEach(suggestion => {
                    if (!suggestion || participants.has(String(suggestion.id))) {
                        return;
                    }

                    const item = document.createElement('button');
                    item.type = 'button';
                    item.className = 'list-group-item list-group-item-action d-flex justify-content-between align-items-center';
                    item.dataset.suggestionId = suggestion.id;
                    item.innerHTML = `
                        <div>
                            <div class="fw-semibold">${suggestion.displayName}</div>
                            <div class="text-muted small">${suggestion.email}</div>
                        </div>
                        <span class="badge bg-primary">Add</span>
                    `;
                    item.addEventListener('click', () => addParticipant({
                        id: String(suggestion.id),
                        displayName: suggestion.displayName,
                        email: suggestion.email
                    }));
                    resultsContainer.appendChild(item);
                });

                resultsContainer.classList.remove('d-none');
            } catch (error) {
                console.error('Unable to search contacts', error);
            }
        }, 250);

        searchInput.addEventListener('input', () => performSearch());
        document.addEventListener('click', (event) => {
            if (!resultsContainer.classList.contains('d-none')) {
                const target = event.target;
                if (target instanceof Node && !resultsContainer.contains(target) && target !== searchInput) {
                    hideResults();
                }
            }
        });
    }

    window.initMeetingParticipants = init;
})();
