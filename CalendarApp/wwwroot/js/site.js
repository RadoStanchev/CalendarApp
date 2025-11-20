(function () {
    const panel = document.querySelector('[data-notification-panel]');
    if (!panel) return;

    const items = panel.querySelector('[data-notification-items]');
    const emptyState = panel.querySelector('[data-notification-empty]');
    const badgeEls = document.querySelectorAll('[data-notification-count]');
    const closeBtn = panel.querySelector('[data-notification-close]');
    const recentUrl = panel.dataset.recentUrl;
    const markUrl = panel.dataset.markUrl;
    const listUrl = panel.dataset.listUrl;
    const antiforgery = document.querySelector('#notification-antiforgery-form input[name="__RequestVerificationToken"]')?.value || '';
    const notificationLinks = document.querySelectorAll('[data-notification-link]');

    let hideTimer = null;

    const TYPE_EMOJI = {
        info: 'ℹ️',
        warning: '⚠️',
        invitation: '🤝',
        reminder: '⏰'
    };

    // ------------------------------------------------
    // UI Helpers
    // ------------------------------------------------
    function showPanel() {
        panel.classList.remove('notification-panel--hidden');
        scheduleHide();
    }

    function hidePanel() {
        panel.classList.add('notification-panel--hidden');
        clearTimeout(hideTimer);
    }

    function scheduleHide() {
        clearTimeout(hideTimer);
        hideTimer = setTimeout(hidePanel, 20000);
    }

    function updateBadge(count) {
        const value = Number(count) || 0;
        badgeEls.forEach(el => {
            el.textContent = value;
            el.classList.toggle('d-none', value === 0);
        });
    }

    function updateEmptyState() {
        const hasItems = items.children.length > 0;
        emptyState.classList.toggle('d-none', hasItems);
        items.classList.toggle('d-none', !hasItems);
        if (!hasItems) hidePanel();
    }

    function relativeTime(dateString) {
        const date = new Date(dateString);
        if (isNaN(date)) return '';

        const diff = (Date.now() - date) / 1000;
        if (diff < 60) return 'Току-що';
        if (diff < 3600) return `преди ${Math.floor(diff / 60)} минути`;
        if (diff < 86400) return `преди ${Math.floor(diff / 3600)} часа`;
        return `преди ${Math.floor(diff / 86400)} дни`;
    }

    function createItem(n) {
        const li = document.createElement('li');
        const type = (n.type || 'info').toLowerCase();
        li.className = `notification-preview-item ${n.isRead ? 'is-read' : ''}`;
        li.dataset.notificationId = n.id;

        li.innerHTML = `
            <div class="notification-preview-item__icon notification-type-${type}">
                ${TYPE_EMOJI[type] || TYPE_EMOJI.info}
            </div>
            <div class="notification-preview-item__content">
                <div class="notification-preview-item__message">${n.message}</div>
                <div class="notification-preview-item__time">${relativeTime(n.createdAt)}</div>
            </div>
        `;

        return li;
    }

    function setItems(list) {
        items.innerHTML = '';
        list.forEach(n => items.appendChild(createItem(n)));
        updateEmptyState();
    }

    function upsert(n) {
        const existing = items.querySelector(`[data-notification-id="${n.id}"]`);
        const el = createItem(n);

        if (existing) {
            items.replaceChild(el, existing);
        } else {
            items.prepend(el);
        }

        showPanel();

        while (items.children.length > 3) {
            items.removeChild(items.lastElementChild);
        }

        updateEmptyState();
    }

    async function markAsRead(id, item) {
        if (!antiforgery || !markUrl) return;
        try {
            const res = await fetch(markUrl, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': antiforgery
                },
                body: JSON.stringify({ id })
            });

            if (!res.ok) return;
            const data = await res.json();
            item.classList.add('is-read');
            updateBadge(data.unreadCount);
        } catch { }
    }

    panel.addEventListener('click', (ev) => {
        const item = ev.target.closest('.notification-preview-item');
        if (!item) return;

        const id = item.dataset.notificationId;

        if (!item.classList.contains('is-read')) markAsRead(id, item);

        if (listUrl) window.location.href = listUrl;
    });

    panel.addEventListener('keydown', (ev) => {
        if (ev.key !== 'Enter' && ev.key !== ' ') return;
        const item = ev.target.closest('.notification-preview-item');
        if (!item) return;
        ev.preventDefault();
        const id = item.dataset.notificationId;
        if (!item.classList.contains('is-read')) markAsRead(id, item);
        if (listUrl) window.location.href = listUrl;
    });

    if (closeBtn) {
        closeBtn.addEventListener('click', (ev) => {
            ev.stopPropagation();
            hidePanel();
        });
    }

    notificationLinks.forEach(link =>
        link.addEventListener('click', () => {
            items.querySelectorAll('.notification-preview-item').forEach(i => i.classList.add('is-read'));
            updateBadge(0);
        })
    );

    async function loadInitial() {
        if (!recentUrl) return;
        try {
            const res = await fetch(recentUrl, { headers: { 'Accept': 'application/json' } });
            if (!res.ok) return;

            const data = await res.json();
            setItems(data.notifications || []);
            updateBadge(data.unreadCount || 0);
        } catch { }
    }

    function initHub() {
        if (!window.signalR) return;

        const connection = new signalR.HubConnectionBuilder()
            .withUrl('/hubs/notifications')
            .withAutomaticReconnect()
            .build();


        connection.on('ReceiveNotification', payload => {
            const n = payload.notification || payload;
            if (n) upsert(n);
            updateBadge(payload.unreadCount);
        });

        connection.on('UnreadCountChanged', count => updateBadge(count));

        connection.start().catch(() => { });
    }

    loadInitial()       
        .catch(() => { })
        .finally(() => initHub());
})();
