(function () {
    function debounce(fn, delay) {
        let timerId;
        return (...args) => {
            window.clearTimeout(timerId);
            timerId = window.setTimeout(() => fn.apply(null, args), delay);
        };
    }

    const STATUS_METADATA = {
        Friend: { text: 'Вече сте приятели', className: 'badge rounded-pill bg-success-subtle text-success fw-semibold' },
        IncomingRequest: { text: 'Входяща покана', className: 'badge rounded-pill bg-warning-subtle text-warning fw-semibold' },
        OutgoingRequest: { text: 'Покана изпратена', className: 'badge rounded-pill bg-primary-subtle text-primary fw-semibold' },
        Blocked: { text: 'Недостъпно', className: 'badge rounded-pill bg-secondary-subtle text-secondary fw-semibold' }
    };

    function buildStatusBadge(status) {
        const meta = STATUS_METADATA[status] ?? { text: status ?? 'Недостъпно', className: 'badge rounded-pill bg-secondary-subtle text-secondary fw-semibold' };
        const badge = document.createElement('span');
        badge.className = meta.className;
        badge.textContent = meta.text;
        return badge;
    }

    function getAntiForgeryToken(root) {
        const localToken = root.querySelector('[data-friend-antiforgery]')?.value ?? '';
        if (localToken) {
            return localToken;
        }

        return document.querySelector('input[name="__RequestVerificationToken"]')?.value ?? '';
    }

    function showFeedback(feedbackElement, message, variant) {
        if (!feedbackElement || !message) {
            return;
        }

        feedbackElement.classList.remove('d-none', 'alert-info', 'alert-success', 'alert-danger');
        feedbackElement.classList.add(`alert-${variant}`);
        feedbackElement.textContent = message;
    }

    function clearFeedback(feedbackElement) {
        if (!feedbackElement) {
            return;
        }

        feedbackElement.classList.add('d-none');
        feedbackElement.classList.remove('alert-info', 'alert-success', 'alert-danger');
        feedbackElement.textContent = '';
    }

    function hideResults(resultsContainer) {
        if (!resultsContainer) {
            return;
        }

        resultsContainer.classList.add('d-none');
        resultsContainer.innerHTML = '';
    }

    function renderResults(resultsContainer, suggestions, handlers) {
        if (!resultsContainer) {
            return;
        }

        resultsContainer.innerHTML = '';

        if (!Array.isArray(suggestions) || suggestions.length === 0) {
            const emptyItem = document.createElement('div');
            emptyItem.className = 'list-group-item text-muted small';
            emptyItem.textContent = 'Няма намерени съвпадения.';
            resultsContainer.appendChild(emptyItem);
            resultsContainer.classList.remove('d-none');
            return;
        }

        suggestions.forEach(suggestion => {
            if (!suggestion?.id) {
                return;
            }

            const item = document.createElement('div');
            item.className = 'list-group-item';

            const content = document.createElement('div');
            content.className = 'friendship-search-details';
            content.innerHTML = `
                <div class="fw-semibold">${suggestion.displayName ?? ''}</div>
                <div class="text-muted small">${suggestion.email ?? ''}</div>
            `;
            item.appendChild(content);

            const actionContainer = document.createElement('div');
            actionContainer.className = 'friendship-search-actions d-flex flex-wrap align-items-center gap-2 ms-sm-auto';

            const status = suggestion.status ?? 'None';
            const hasStatus = status && status !== 'None';
            const statusBadge = hasStatus ? buildStatusBadge(status) : null;

            switch (status) {
                case 'Friend':
                    if (statusBadge) {
                        actionContainer.appendChild(statusBadge);
                    }
                    if (typeof handlers?.onRemove === 'function') {
                        const removeButton = document.createElement('button');
                        removeButton.type = 'button';
                        removeButton.className = 'btn btn-sm btn-outline-danger';
                        removeButton.textContent = 'Премахни приятел';
                        removeButton.addEventListener('click', () => handlers.onRemove(suggestion, removeButton));
                        actionContainer.appendChild(removeButton);
                    }
                    break;
                case 'IncomingRequest':
                    if (statusBadge) {
                        actionContainer.appendChild(statusBadge);
                    }
                    if (typeof handlers?.onAccept === 'function' && typeof handlers?.onDecline === 'function' && suggestion.friendshipId) {
                        const confirmButton = document.createElement('button');
                        confirmButton.type = 'button';
                        confirmButton.className = 'btn btn-sm btn-primary';
                        confirmButton.textContent = 'Потвърди';

                        const ignoreButton = document.createElement('button');
                        ignoreButton.type = 'button';
                        ignoreButton.className = 'btn btn-sm btn-outline-secondary';
                        ignoreButton.textContent = 'Игнорирай';

                        confirmButton.addEventListener('click', () => handlers.onAccept(suggestion, { confirmButton, ignoreButton }));
                        ignoreButton.addEventListener('click', () => handlers.onDecline(suggestion, { confirmButton, ignoreButton }));

                        actionContainer.appendChild(confirmButton);
                        actionContainer.appendChild(ignoreButton);
                    }
                    break;
                case 'OutgoingRequest':
                    if (statusBadge) {
                        actionContainer.appendChild(statusBadge);
                    }
                    if (typeof handlers?.onCancel === 'function' && suggestion.friendshipId) {
                        const cancelButton = document.createElement('button');
                        cancelButton.type = 'button';
                        cancelButton.className = 'btn btn-sm btn-outline-danger';
                        cancelButton.textContent = 'Отмени поканата';
                        cancelButton.addEventListener('click', () => handlers.onCancel(suggestion, cancelButton));
                        actionContainer.appendChild(cancelButton);
                    }
                    break;
                case 'Blocked':
                    if (statusBadge) {
                        actionContainer.appendChild(statusBadge);
                    }
                    break;
                default:
                    if (typeof handlers?.onSend === 'function') {
                        const actionButton = document.createElement('button');
                        actionButton.type = 'button';
                        actionButton.className = 'btn btn-sm btn-primary';
                        actionButton.textContent = 'Добави приятел';
                        actionButton.addEventListener('click', () => handlers.onSend(suggestion, actionButton));
                        actionContainer.appendChild(actionButton);
                    }
                    break;
            }

            if (actionContainer.childElementCount > 0) {
                item.appendChild(actionContainer);
            }

            resultsContainer.appendChild(item);
        });

        resultsContainer.classList.remove('d-none');
    }

    function initFriendSearch(root) {
        if (!root) {
            return;
        }

        const searchUrl = root.dataset.searchUrl;
        const requestUrl = root.dataset.requestUrl;
        const removeUrl = root.dataset.removeUrl;
        const acceptUrl = root.dataset.acceptUrl;
        const declineUrl = root.dataset.declineUrl;
        const cancelUrl = root.dataset.cancelUrl;
        const searchInput = root.querySelector('[data-friend-search]');
        const resultsContainer = root.querySelector('[data-friend-search-results]');
        const feedbackElement = root.querySelector('[data-friend-search-feedback]');
        const antiForgeryToken = getAntiForgeryToken(root);

        if (!searchUrl || !searchInput || !resultsContainer) {
            return;
        }

        let queryToken = 0;

        let latestResults = [];
        const actionHandlers = {};

        function refreshResults() {
            renderResults(resultsContainer, latestResults, actionHandlers);
        }

        function updateSuggestion(suggestion, status, friendshipId, isIncoming) {
            if (!suggestion) {
                return;
            }

            suggestion.status = status ?? 'None';
            suggestion.friendshipId = friendshipId ?? null;
            suggestion.isIncomingRequest = Boolean(isIncoming);
            refreshResults();
        }

        async function postAction(url, payload, options = {}) {
            const { successMessage, errorMessage } = options;

            if (!url) {
                return {
                    success: false,
                    message: errorMessage ?? 'Действието не е налично в момента.'
                };
            }

            if (!antiForgeryToken) {
                return {
                    success: false,
                    message: 'Не успяхме да валидираме заявката. Опреснете страницата и опитайте отново.'
                };
            }

            const body = new URLSearchParams();
            Object.entries(payload ?? {}).forEach(([key, value]) => {
                if (value !== undefined && value !== null) {
                    body.append(key, value.toString());
                }
            });

            try {
                const response = await fetch(url, {
                    method: 'POST',
                    headers: {
                        'Accept': 'application/json',
                        'Content-Type': 'application/x-www-form-urlencoded; charset=UTF-8',
                        'X-Requested-With': 'XMLHttpRequest',
                        'RequestVerificationToken': antiForgeryToken
                    },
                    body
                });

                if (!response.ok) {
                    throw new Error('Заявката беше неуспешна');
                }

                const data = await response.json();
                const success = Boolean(data?.success);
                const message = data?.message ?? (success ? successMessage : errorMessage) ?? '';
                return { ...data, success, message };
            } catch (error) {
                console.warn('Неуспешно действие за приятелство', error);
                return {
                    success: false,
                    message: errorMessage ?? 'Действието не можа да бъде изпълнено. Моля, опитайте отново.'
                };
            }
        }

        async function sendFriendRequest(suggestion, button) {
            if (!button || !suggestion?.id) {
                return;
            }

            button.disabled = true;
            const originalLabel = button.textContent;
            button.textContent = 'Изпращане…';

            const result = await postAction(requestUrl, { receiverId: suggestion.id }, {
                successMessage: 'Поканата за приятелство е изпратена.',
                errorMessage: 'Поканата за приятелство не може да бъде изпратена.'
            });

            if (result.success) {
                showFeedback(feedbackElement, result.message, 'success');
                updateSuggestion(suggestion, 'OutgoingRequest', result.friendshipId ?? suggestion.friendshipId ?? null, false);
            } else {
                showFeedback(feedbackElement, result.message, 'danger');
                button.disabled = false;
                button.textContent = originalLabel;
            }
        }

        async function removeFriend(suggestion, button) {
            if (!button || !suggestion?.friendshipId) {
                return;
            }

            const originalLabel = button.textContent;
            button.disabled = true;
            button.textContent = 'Премахване…';

            const result = await postAction(removeUrl, { friendshipId: suggestion.friendshipId }, {
                successMessage: 'Приятелят беше премахнат.',
                errorMessage: 'Приятелят не може да бъде премахнат.'
            });

            if (result.success) {
                showFeedback(feedbackElement, result.message, 'success');
                updateSuggestion(suggestion, 'None', null, false);
            } else {
                showFeedback(feedbackElement, result.message, 'danger');
                button.disabled = false;
                button.textContent = originalLabel;
            }
        }

        async function cancelFriendRequest(suggestion, button) {
            if (!button || !suggestion?.friendshipId) {
                showFeedback(feedbackElement, 'Не успяхме да намерим тази покана. Опреснете страницата и опитайте отново.', 'danger');
                return;
            }

            const originalLabel = button.textContent;
            button.disabled = true;
            button.textContent = 'Отмяна…';

            const result = await postAction(cancelUrl, { friendshipId: suggestion.friendshipId }, {
                successMessage: 'Поканата за приятелство е отменена.',
                errorMessage: 'Тази покана не може да бъде отменена.'
            });

            if (result.success) {
                showFeedback(feedbackElement, result.message, 'success');
                updateSuggestion(suggestion, 'None', null, false);
            } else {
                showFeedback(feedbackElement, result.message, 'danger');
                button.disabled = false;
                button.textContent = originalLabel;
            }
        }

        async function acceptFriendRequest(suggestion, controls) {
            if (!suggestion?.friendshipId) {
                showFeedback(feedbackElement, 'Не успяхме да намерим тази покана. Опреснете страницата и опитайте отново.', 'danger');
                return;
            }

            const confirmButton = controls?.confirmButton;
            const ignoreButton = controls?.ignoreButton;
            const originalConfirmLabel = confirmButton?.textContent ?? '';

            if (confirmButton) {
                confirmButton.disabled = true;
                confirmButton.textContent = 'Потвърждаване…';
            }

            if (ignoreButton) {
                ignoreButton.disabled = true;
            }

            const result = await postAction(acceptUrl, { friendshipId: suggestion.friendshipId }, {
                successMessage: 'Поканата за приятелство е приета.',
                errorMessage: 'Тази покана не може да бъде приета.'
            });

            if (result.success) {
                showFeedback(feedbackElement, result.message, 'success');
                updateSuggestion(suggestion, 'Friend', null, false);
            } else {
                showFeedback(feedbackElement, result.message, 'danger');
                if (confirmButton) {
                    confirmButton.disabled = false;
                    confirmButton.textContent = originalConfirmLabel;
                }
                if (ignoreButton) {
                    ignoreButton.disabled = false;
                }
            }
        }

        async function declineFriendRequest(suggestion, controls) {
            if (!suggestion?.friendshipId) {
                showFeedback(feedbackElement, 'Не успяхме да намерим тази покана. Опреснете страницата и опитайте отново.', 'danger');
                return;
            }

            const ignoreButton = controls?.ignoreButton;
            const confirmButton = controls?.confirmButton;
            const originalIgnoreLabel = ignoreButton?.textContent ?? '';

            if (ignoreButton) {
                ignoreButton.disabled = true;
                ignoreButton.textContent = 'Игнориране…';
            }

            if (confirmButton) {
                confirmButton.disabled = true;
            }

            const result = await postAction(declineUrl, { friendshipId: suggestion.friendshipId }, {
                successMessage: 'Поканата за приятелство е отказана.',
                errorMessage: 'Тази покана не може да бъде отказана.'
            });

            if (result.success) {
                showFeedback(feedbackElement, result.message, 'success');
                updateSuggestion(suggestion, 'None', null, false);
            } else {
                showFeedback(feedbackElement, result.message, 'danger');
                if (ignoreButton) {
                    ignoreButton.disabled = false;
                    ignoreButton.textContent = originalIgnoreLabel;
                }
                if (confirmButton) {
                    confirmButton.disabled = false;
                }
            }
        }

        actionHandlers.onSend = sendFriendRequest;
        actionHandlers.onRemove = removeFriend;
        actionHandlers.onCancel = cancelFriendRequest;
        actionHandlers.onAccept = acceptFriendRequest;
        actionHandlers.onDecline = declineFriendRequest;

        const performSearch = debounce(async () => {
            const term = searchInput.value.trim();
            if (term.length < 2) {
                hideResults(resultsContainer);
                clearFeedback(feedbackElement);
                latestResults = [];
                return;
            }

            const token = ++queryToken;
            const url = new URL(searchUrl, window.location.origin);
            url.searchParams.set('term', term);

            try {
                const response = await fetch(url.toString(), { headers: { 'Accept': 'application/json' } });
                if (!response.ok || token !== queryToken) {
                    return;
                }

                const suggestions = await response.json();
                latestResults = Array.isArray(suggestions) ? suggestions : [];
                refreshResults();
            } catch (error) {
                console.warn('Неуспешно търсене на хора', error);
                showFeedback(feedbackElement, 'Възникна проблем при търсенето. Моля, опитайте отново.', 'danger');
            }
        }, 250);

        searchInput.addEventListener('input', () => {
            clearFeedback(feedbackElement);
            performSearch();
        });

        document.addEventListener('click', (event) => {
            if (!resultsContainer.classList.contains('d-none')) {
                const target = event.target;
                if (target instanceof Node && !resultsContainer.contains(target) && target !== searchInput) {
                    hideResults(resultsContainer);
                }
            }
        });
    }

    function initTogglePanels(root) {
        if (!root) {
            return;
        }

        const buttons = Array.from(root.querySelectorAll('[data-friend-panel-toggle]'));
        const panels = new Map();

        root.querySelectorAll('[data-friend-panel]').forEach(panel => {
            const key = panel.dataset.friendPanel;
            if (key) {
                panels.set(key, panel);
            }
        });

        function activate(targetKey) {
            panels.forEach((panel, key) => {
                if (key === targetKey) {
                    panel.classList.remove('d-none');
                } else {
                    panel.classList.add('d-none');
                }
            });

            buttons.forEach(button => {
                if (button.dataset.friendPanelToggle === targetKey) {
                    button.classList.add('active');
                    button.setAttribute('aria-pressed', 'true');
                } else {
                    button.classList.remove('active');
                    button.setAttribute('aria-pressed', 'false');
                }
            });
        }

        buttons.forEach(button => {
            button.addEventListener('click', () => {
                if (button.classList.contains('active')) {
                    return;
                }

                const targetKey = button.dataset.friendPanelToggle;
                if (targetKey && panels.has(targetKey)) {
                    activate(targetKey);
                }
            });
        });

        const defaultButton = buttons.find(button => button.classList.contains('active')) ?? buttons[0];
        const defaultKey = defaultButton?.dataset.friendPanelToggle;
        if (defaultKey && panels.has(defaultKey)) {
            activate(defaultKey);
        }
    }

    function initAll() {
        document.querySelectorAll('[data-friend-search-root]').forEach(root => initFriendSearch(root));
        document.querySelectorAll('[data-friend-toggle-root]').forEach(root => initTogglePanels(root));
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initAll);
    } else {
        initAll();
    }

    window.initFriendships = initAll;
})();
