// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

(function () {
    const panel = document.querySelector('[data-notification-panel]');
    if (!panel) {
        return;
    }

    const itemsContainer = panel.querySelector('[data-notification-items]');
    const emptyState = panel.querySelector('[data-notification-empty]');
    const countBadges = document.querySelectorAll('[data-notification-count]');
    const closeButton = panel.querySelector('[data-notification-close]');
    const recentUrl = panel.getAttribute('data-recent-url');
    const markUrl = panel.getAttribute('data-mark-url');
    const listUrl = panel.getAttribute('data-list-url');
    const antiForgeryToken = document.querySelector('#notification-antiforgery-form input[name="__RequestVerificationToken"]')?.value ?? '';
    const notificationLinks = document.querySelectorAll('[data-notification-link]');
    const listPathname = (() => {
        if (!listUrl) {
            return null;
        }

        try {
            return new URL(listUrl, window.location.origin).pathname.toLowerCase();
        } catch (error) {
            console.warn('Failed to parse notifications list URL', error);
            return null;
        }
    })();

    let hideTimeoutId = null;

    const typeEmojis = {
        info: 'ℹ️',
        warning: '⚠️',
        invitation: '🤝',
        reminder: '⏰'
    };

    const typeClass = (type) => `notification-type-${type?.toString().toLowerCase() ?? 'info'}`;

    function hidePanel() {
        if (!panel) {
            return;
        }

        panel.classList.add('notification-panel--hidden');
        window.clearTimeout(hideTimeoutId);
        hideTimeoutId = null;
    }

    function scheduleAutoHide() {
        window.clearTimeout(hideTimeoutId);
        hideTimeoutId = window.setTimeout(() => {
            hidePanel();
        }, 5000);
    }

    function showPanel() {
        if (!panel) {
            return;
        }

        panel.classList.remove('notification-panel--hidden');
        scheduleAutoHide();
    }

    function updateBadge(unreadCount) {
        if (!countBadges || countBadges.length === 0) {
            return;
        }

        const value = Number.isFinite(unreadCount) ? unreadCount : 0;
        countBadges.forEach((badge) => {
            badge.textContent = value.toString();
            badge.classList.toggle('d-none', value === 0);
        });
    }

    function updateEmptyState() {
        if (!emptyState || !itemsContainer) {
            return;
        }

        const hasItems = itemsContainer.children.length > 0;
        emptyState.classList.toggle('d-none', hasItems);
        itemsContainer.classList.toggle('d-none', !hasItems);

        if (hasItems) {
            showPanel();
        } else {
            hidePanel();
        }
    }

    function formatRelativeTime(dateString) {
        if (!dateString) {
            return '';
        }

        const date = new Date(dateString);
        if (Number.isNaN(date.getTime())) {
            return '';
        }

        const diff = (Date.now() - date.getTime()) / 1000;
        if (diff < 60) {
            return 'Just now';
        }

        if (diff < 3600) {
            const minutes = Math.floor(diff / 60);
            return `${minutes} minute${minutes === 1 ? '' : 's'} ago`;
        }

        if (diff < 86400) {
            const hours = Math.floor(diff / 3600);
            return `${hours} hour${hours === 1 ? '' : 's'} ago`;
        }

        const days = Math.floor(diff / 86400);
        return `${days} day${days === 1 ? '' : 's'} ago`;
    }

    function buildPreviewItem(notification) {
        const li = document.createElement('li');
        li.className = `notification-preview-item ${notification.isRead ? 'is-read' : ''}`.trim();
        li.dataset.notificationId = notification.id;
        li.setAttribute('role', 'button');
        li.tabIndex = 0;

        const type = (notification.type ?? 'info').toString().toLowerCase();
        const emoji = typeEmojis[type] ?? typeEmojis.info;

        li.innerHTML = `
            <div class="notification-preview-item__icon ${typeClass(type)}">${emoji}</div>
            <div class="notification-preview-item__content">
                <div class="notification-preview-item__message">${notification.message ?? ''}</div>
                <div class="notification-preview-item__time">${formatRelativeTime(notification.createdAt)}</div>
            </div>
        `;

        return li;
    }

    function setNotifications(notifications) {
        if (!itemsContainer) {
            return;
        }

        itemsContainer.innerHTML = '';
        notifications.forEach((notification) => {
            const item = buildPreviewItem(notification);
            itemsContainer.appendChild(item);
        });
        updateEmptyState();
    }

    function upsertNotification(notification) {
        if (!itemsContainer) {
            return;
        }

        const existing = itemsContainer.querySelector(`[data-notification-id="${notification.id}"]`);
        const item = buildPreviewItem(notification);

        if (existing) {
            itemsContainer.replaceChild(item, existing);
        } else {
            itemsContainer.prepend(item);
        }

        while (itemsContainer.children.length > 5) {
            itemsContainer.removeChild(itemsContainer.lastElementChild);
        }

        updateEmptyState();
    }

    async function markAsRead(id, element) {
        if (!id || !antiForgeryToken) {
            return;
        }

        try {
            const response = await fetch(markUrl, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': antiForgeryToken
                },
                body: JSON.stringify({ id })
            });

            if (!response.ok) {
                return;
            }

            const payload = await response.json();
            element?.classList.add('is-read');
            updateBadge(payload.unreadCount);
        } catch (error) {
            console.warn('Failed to mark notification as read', error);
        }
    }

    function markAllLocalAsRead() {
        if (!itemsContainer) {
            return;
        }

        itemsContainer.querySelectorAll('.notification-preview-item').forEach((item) => {
            item.classList.add('is-read');
        });
    }

    function handleNotificationLinkInteractions() {
        if (!notificationLinks || notificationLinks.length === 0) {
            return;
        }

        notificationLinks.forEach((link) => {
            link.addEventListener('click', () => {
                markAllLocalAsRead();
                updateBadge(0);
            });
        });
    }

    function isOnNotificationListPage() {
        if (!listPathname) {
            return false;
        }

        try {
            return window.location.pathname.toLowerCase() === listPathname;
        } catch (error) {
            console.warn('Failed to determine current path', error);
            return false;
        }
    }

    function bindInteractions() {
        panel.addEventListener('click', (event) => {
            const item = event.target.closest('.notification-preview-item');
            if (!item) {
                return;
            }

            const notificationId = item.dataset.notificationId;
            if (!item.classList.contains('is-read')) {
                markAsRead(notificationId, item);
            }

            if (listUrl) {
                window.location.href = listUrl;
            }
        });

        panel.addEventListener('keydown', (event) => {
            if (event.key !== 'Enter' && event.key !== ' ') {
                return;
            }

            const item = event.target.closest('.notification-preview-item');
            if (!item) {
                return;
            }

            event.preventDefault();
            const notificationId = item.dataset.notificationId;
            if (!item.classList.contains('is-read')) {
                markAsRead(notificationId, item);
            }
            if (listUrl) {
                window.location.href = listUrl;
            }
        });
    }

    if (closeButton) {
        closeButton.addEventListener('click', (event) => {
            event.stopPropagation();
            hidePanel();
        });
    }

    async function loadInitialAsync() {
        if (!recentUrl) {
            return;
        }

        try {
            const response = await fetch(recentUrl, { headers: { 'Accept': 'application/json' } });
            if (!response.ok) {
                return;
            }

            const payload = await response.json();
            const notifications = payload?.notifications ?? payload ?? [];
            setNotifications(notifications);
            updateBadge(payload?.unreadCount ?? 0);
        } catch (error) {
            console.warn('Failed to load notifications', error);
        }
    }

    function initialiseHub() {
        if (!window.signalR || typeof window.signalR.HubConnectionBuilder !== 'function') {
            return;
        }

        const connection = new window.signalR.HubConnectionBuilder()
            .withUrl('/hubs/notifications')
            .withAutomaticReconnect()
            .build();

        connection.on('ReceiveNotification', (payload) => {
            if (!payload) {
                return;
            }

            const notification = payload.notification ?? payload;
            if (notification) {
                upsertNotification(notification);
            }

            if (typeof payload.unreadCount === 'number') {
                updateBadge(payload.unreadCount);
            } else if (typeof payload.notification?.unreadCount === 'number') {
                updateBadge(payload.notification.unreadCount);
            }
        });

        connection.on('UnreadCountChanged', (count) => {
            updateBadge(count);
        });

        connection.start().catch((error) => {
            console.warn('SignalR connection failed', error);
        });
    }

    bindInteractions();
    handleNotificationLinkInteractions();

    if (isOnNotificationListPage()) {
        markAllLocalAsRead();
        updateBadge(0);
    }

    loadInitialAsync()
        .then(() => {
            if (isOnNotificationListPage()) {
                markAllLocalAsRead();
                updateBadge(0);
            }
        })
        .finally(initialiseHub);
})();
