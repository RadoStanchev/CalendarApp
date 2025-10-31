(function () {
    function debounce(fn, delay) {
        let timerId;
        return (...args) => {
            window.clearTimeout(timerId);
            timerId = window.setTimeout(() => fn.apply(null, args), delay);
        };
    }

    const STATUS_METADATA = {
        Friend: { text: 'Already friends', className: 'badge rounded-pill bg-success-subtle text-success fw-semibold' },
        IncomingRequest: { text: 'Incoming request', className: 'badge rounded-pill bg-warning-subtle text-warning fw-semibold' },
        OutgoingRequest: { text: 'Request sent', className: 'badge rounded-pill bg-primary-subtle text-primary fw-semibold' },
        Blocked: { text: 'Unavailable', className: 'badge rounded-pill bg-secondary-subtle text-secondary fw-semibold' }
    };

    function buildStatusBadge(status) {
        const meta = STATUS_METADATA[status] ?? { text: status ?? 'Unavailable', className: 'badge rounded-pill bg-secondary-subtle text-secondary fw-semibold' };
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
            emptyItem.textContent = 'No matches found.';
            resultsContainer.appendChild(emptyItem);
            resultsContainer.classList.remove('d-none');
            return;
        }

        suggestions.forEach(suggestion => {
            if (!suggestion?.id) {
                return;
            }

            const item = document.createElement('div');
            item.className = 'list-group-item d-flex align-items-start justify-content-between gap-3';

            const content = document.createElement('div');
            content.innerHTML = `
                <div class="fw-semibold">${suggestion.displayName ?? ''}</div>
                <div class="text-muted small">${suggestion.email ?? ''}</div>
            `;
            item.appendChild(content);

            if (suggestion.status && suggestion.status !== 'None') {
                item.appendChild(buildStatusBadge(suggestion.status));
            } else {
                const actionButton = document.createElement('button');
                actionButton.type = 'button';
                actionButton.className = 'btn btn-sm btn-primary';
                actionButton.textContent = 'Add friend';
                actionButton.addEventListener('click', () => handlers.onSend?.(suggestion, actionButton));
                item.appendChild(actionButton);
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
        const searchInput = root.querySelector('[data-friend-search]');
        const resultsContainer = root.querySelector('[data-friend-search-results]');
        const feedbackElement = root.querySelector('[data-friend-search-feedback]');
        const antiForgeryToken = getAntiForgeryToken(root);
        const excludeSet = new Set((root.dataset.exclude ?? '')
            .split(',')
            .map(value => value.trim())
            .filter(value => value.length > 0));

        if (!searchUrl || !searchInput || !resultsContainer) {
            return;
        }

        let queryToken = 0;

        async function sendFriendRequest(suggestion, button) {
            if (!requestUrl || !suggestion?.id) {
                return;
            }

            const token = antiForgeryToken;
            if (!token) {
                showFeedback(feedbackElement, 'We could not validate your request. Please refresh and try again.', 'danger');
                return;
            }

            button.disabled = true;
            const originalLabel = button.textContent;
            button.textContent = 'Sending…';

            try {
                const response = await fetch(requestUrl, {
                    method: 'POST',
                    headers: {
                        'Accept': 'application/json',
                        'Content-Type': 'application/x-www-form-urlencoded; charset=UTF-8',
                        'X-Requested-With': 'XMLHttpRequest',
                        'RequestVerificationToken': token
                    },
                    body: new URLSearchParams({ receiverId: suggestion.id })
                });

                if (!response.ok) {
                    throw new Error('Request failed');
                }

                const payload = await response.json();
                if (payload?.success) {
                    showFeedback(feedbackElement, payload?.message ?? 'Friend request sent.', 'success');
                    excludeSet.add(String(suggestion.id));
                    root.dataset.exclude = Array.from(excludeSet).join(',');
                    const badge = buildStatusBadge('OutgoingRequest');
                    button.replaceWith(badge);
                } else {
                    showFeedback(feedbackElement, payload?.message ?? 'Unable to send friend request.', 'danger');
                    button.disabled = false;
                    button.textContent = originalLabel;
                }
            } catch (error) {
                console.warn('Failed to send friend request', error);
                showFeedback(feedbackElement, 'Unable to send friend request right now.', 'danger');
                button.disabled = false;
                button.textContent = originalLabel;
            }
        }

        const performSearch = debounce(async () => {
            const term = searchInput.value.trim();
            if (term.length < 2) {
                hideResults(resultsContainer);
                clearFeedback(feedbackElement);
                return;
            }

            const token = ++queryToken;
            const url = new URL(searchUrl, window.location.origin);
            url.searchParams.set('term', term);

            if (excludeSet.size > 0) {
                url.searchParams.set('exclude', Array.from(excludeSet).join(','));
            }

            try {
                const response = await fetch(url.toString(), { headers: { 'Accept': 'application/json' } });
                if (!response.ok || token !== queryToken) {
                    return;
                }

                const suggestions = await response.json();
                renderResults(resultsContainer, suggestions, {
                    onSend: sendFriendRequest
                });
            } catch (error) {
                console.warn('Unable to search for people', error);
                showFeedback(feedbackElement, 'Something went wrong while searching. Please try again.', 'danger');
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

    function initAll() {
        document.querySelectorAll('[data-friend-search-root]').forEach(root => initFriendSearch(root));
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initAll);
    } else {
        initAll();
    }

    window.initFriendships = initAll;
})();
