(() => {
    const messagesContainer = document.getElementById("chatMessages");
    const messageInput = document.getElementById("messageInput");
    const sendButton = document.getElementById("sendButton");
    const chatForm = document.getElementById("chatForm");
    const searchInput = document.getElementById("chatSearch");
    const activeContactName = document.getElementById("activeContactName");
    const activeContactStatus = document.getElementById("activeContactStatus");
    const activeContactAvatar = document.getElementById("activeContactAvatar");

    if (!messagesContainer || !chatForm || !sendButton || !window.signalR) {
        return;
    }

    const THREAD_TYPES = {
        FRIENDSHIP: "friendship",
        MEETING: "meeting"
    };

    const accentClasses = new Set([
        "accent-blue",
        "accent-purple",
        "accent-green",
        "accent-orange",
        "accent-teal"
    ]);

    const normaliseThreadType = (value) => {
        const lowered = (value ?? "").toString().toLowerCase();
        return lowered === THREAD_TYPES.MEETING ? THREAD_TYPES.MEETING : THREAD_TYPES.FRIENDSHIP;
    };

    const getCurrentUserId = () => messagesContainer.dataset.currentUserId ?? "";

    const threadButtons = () => Array.from(document.querySelectorAll(".messenger-thread"));

    const findThreadButton = (threadId, threadType) => {
        const normalisedType = normaliseThreadType(threadType);
        return threadButtons().find(item =>
            item.dataset.threadId === threadId
            && normaliseThreadType(item.dataset.threadType) === normalisedType
        ) ?? null;
    };

    const sidebarSections = Array.from(document.querySelectorAll('[data-thread-section]'));
    const sidebarToggleButtons = Array.from(document.querySelectorAll('[data-thread-filter]'));

    const updateThreadOnlineState = (button, isOnline) => {
        if (!button) {
            return;
        }

        button.dataset.isOnline = isOnline ? "true" : "false";
        button.classList.toggle("messenger-thread--online", Boolean(isOnline));

        const indicator = button.querySelector('[data-online-indicator]');
        if (indicator) {
            indicator.classList.toggle("is-hidden", !isOnline);
        }

        const onlineLabel = button.querySelector('[data-online-label]');
        if (onlineLabel) {
            onlineLabel.textContent = isOnline ? "Онлайн" : "";
        }

        if (button.classList.contains("active") && activeContactStatus) {
            const secondary = button.dataset.secondary || "";
            const status = button.dataset.status || "";
            const statusText = secondary || (isOnline ? "Онлайн" : status);
            activeContactStatus.textContent = statusText;
            activeContactStatus.classList.toggle("text-muted", !statusText);
        }
    };

    let activeSidebarFilter = null;

    const applySidebarFilter = (filter) => {
        if (!sidebarSections.length && !sidebarToggleButtons.length) {
            return;
        }

        const normalised = normaliseThreadType(filter);
        activeSidebarFilter = normalised;

        sidebarSections.forEach(section => {
            const sectionType = normaliseThreadType(section.dataset.threadSection);
            section.classList.toggle("is-collapsed", sectionType !== normalised);
        });

        sidebarToggleButtons.forEach(button => {
            const buttonType = normaliseThreadType(button.dataset.threadFilter);
            const isActive = buttonType === normalised;
            button.classList.toggle("is-active", isActive);
            button.setAttribute("aria-pressed", isActive ? "true" : "false");
        });
    };

    const getAntiForgeryToken = () => chatForm.querySelector('input[name="__RequestVerificationToken"]')?.value ?? '';

    const markCache = new Map();

    const threadTypeValue = (threadType) => normaliseThreadType(threadType) === THREAD_TYPES.MEETING ? 1 : 0;

    const markThreadAsRead = async (threadId, threadType) => {
        if (!threadId) {
            return;
        }

        const token = getAntiForgeryToken();
        if (!token) {
            return;
        }

        const key = `${normaliseThreadType(threadType)}:${threadId}`;
        const now = Date.now();
        const lastMarked = markCache.get(key) ?? 0;

        if (now - lastMarked < 500) {
            return;
        }

        markCache.set(key, now);

        try {
            const params = new URLSearchParams({
                threadId,
                threadType: threadTypeValue(threadType).toString()
            });

            await fetch(`/Chat/MarkThreadAsRead?${params.toString()}`, {
                method: 'POST',
                headers: {
                    'RequestVerificationToken': token,
                    'X-Requested-With': 'XMLHttpRequest'
                }
            });
        } catch (error) {
            console.warn('Failed to mark conversation as read:', error);
        }
    };

    const connection = new signalR.HubConnectionBuilder()
        .withUrl("/hubs/chat")
        .withAutomaticReconnect()
        .build();

    let currentThreadId = messagesContainer.dataset.threadId || null;
    let currentThreadType = currentThreadId ? normaliseThreadType(messagesContainer.dataset.threadType) : null;
    if (!currentThreadId) {
        currentThreadType = null;
    }

    if (sidebarSections.length || sidebarToggleButtons.length) {
        const initialFilterButton = sidebarToggleButtons.find(button => button.classList.contains("is-active"));
        const initialFilter = initialFilterButton
            ? normaliseThreadType(initialFilterButton.dataset.threadFilter)
            : (currentThreadType ?? THREAD_TYPES.FRIENDSHIP);
        applySidebarFilter(initialFilter);
    }

    let connectionStarted = false;
    let pendingJoin = currentThreadId && currentThreadType
        ? { id: currentThreadId, type: currentThreadType }
        : null;

    const setComposerState = (enabled) => {
        if (messageInput) {
            messageInput.disabled = !enabled;
        }
        if (sendButton) {
            sendButton.disabled = !enabled;
        }
    };

    const removeEmptyState = () => {
        if (messagesContainer.dataset.empty !== "true") {
            return;
        }

        const placeholder = messagesContainer.querySelector(".messenger-empty-state");
        if (placeholder) {
            placeholder.remove();
        }

        messagesContainer.dataset.empty = "false";
    };

    const showEmptyState = () => {
        messagesContainer.innerHTML = "";
        const placeholder = document.createElement("div");
        placeholder.className = "messenger-empty-state";
        placeholder.innerHTML = `
            <h3 class="messenger-empty-state__title">Изберете приятел или започнете нов разговор</h3>
            <p class="messenger-empty-state__text">Съобщенията се появяват тук веднага щом някой напише нещо.</p>
        `;
        messagesContainer.appendChild(placeholder);
        messagesContainer.dataset.empty = "true";
    };

    const formatClockTime = (date) => {
        try {
            return new Intl.DateTimeFormat("bg-BG", {
                hour: "2-digit",
                minute: "2-digit"
            }).format(date);
        } catch (error) {
            console.error("Failed to format message time", error);
            return "";
        }
    };

    const formatRelativeTime = (date) => {
        const now = Date.now();
        const diffMs = date.getTime() - now;
        const diffMinutes = Math.round(diffMs / 60000);
        const diffHours = Math.round(diffMs / 3600000);
        const diffDays = Math.round(diffMs / 86400000);
        const rtf = new Intl.RelativeTimeFormat("bg-BG", { numeric: "auto" });

        if (Math.abs(diffMinutes) < 1) {
            return "Сега";
        }

        if (Math.abs(diffMinutes) < 60) {
            return rtf.format(diffMinutes, "minute");
        }

        if (Math.abs(diffHours) < 24) {
            return rtf.format(diffHours, "hour");
        }

        if (Math.abs(diffDays) < 7) {
            return rtf.format(diffDays, "day");
        }

        return date.toLocaleDateString("bg-BG");
    };

    const updateThreadPreview = (threadId, threadType, message) => {
        const button = findThreadButton(threadId, threadType);
        if (!button) {
            return;
        }

        const preview = button.querySelector(".messenger-thread__preview");
        if (preview) {
            preview.textContent = message.content || "";
        }

        const sentAt = message.sentAt ? new Date(message.sentAt) : new Date();

        const meta = button.querySelector(".messenger-thread__meta");
        if (meta) {
            meta.textContent = formatRelativeTime(sentAt);
        }

        button.dataset.lastMessage = message.content || "";
        button.dataset.status = formatRelativeTime(sentAt);
    };

    const applyActiveThread = (button) => {
        threadButtons().forEach(item => item.classList.remove("active"));

        if (!button) {
            if (activeContactName) {
                activeContactName.textContent = "Изберете разговор";
            }

            if (activeContactStatus) {
                activeContactStatus.textContent = "";
                activeContactStatus.classList.add("text-muted");
            }

            if (activeContactAvatar) {
                accentClasses.forEach(cls => activeContactAvatar.classList.remove(cls));
                activeContactAvatar.textContent = "";
                activeContactAvatar.classList.add("accent-blue");
            }

            messagesContainer.dataset.threadId = "";
            messagesContainer.dataset.threadType = "";
            setComposerState(false);
            return;
        }

        const threadType = normaliseThreadType(button.dataset.threadType);
        if (threadType && (sidebarSections.length || sidebarToggleButtons.length)) {
            applySidebarFilter(threadType);
        }

        button.classList.add("active");

        const name = button.dataset.displayName || "Разговор";
        const secondary = button.dataset.secondary || "";
        const status = button.dataset.status || "";
        const initials = button.dataset.avatar || name.substring(0, 2).toUpperCase();
        const accent = button.dataset.accent;
        const isOnline = button.dataset.isOnline === "true";

        if (activeContactName) {
            activeContactName.textContent = name;
        }

        if (activeContactStatus) {
            const statusText = secondary || (isOnline ? "Онлайн" : status);
            activeContactStatus.textContent = statusText;
            activeContactStatus.classList.toggle("text-muted", !statusText);
        }

        if (activeContactAvatar) {
            activeContactAvatar.textContent = initials;
            accentClasses.forEach(cls => activeContactAvatar.classList.remove(cls));
            activeContactAvatar.classList.add(accent && accentClasses.has(accent) ? accent : "accent-blue");
        }

        messagesContainer.dataset.threadId = button.dataset.threadId || "";
        messagesContainer.dataset.threadType = threadType;
        setComposerState(connectionStarted && Boolean(button.dataset.threadId));
    };

    const appendMessage = (message) => {
        removeEmptyState();

        const sentAt = message.sentAt ? new Date(message.sentAt) : new Date();
        const senderId = message.senderId?.toString?.() ?? message.senderId ?? "";
        const isOwn = senderId && senderId.toString() === getCurrentUserId();

        const wrapper = document.createElement("div");
        wrapper.classList.add("message", isOwn ? "message--outgoing" : "message--incoming");

        const bubble = document.createElement("div");
        bubble.classList.add("message__bubble");

        const author = document.createElement("span");
        author.classList.add("message__author");
        author.textContent = isOwn ? "Вие" : (message.senderName || "Потребител");

        const text = document.createElement("p");
        text.classList.add("message__text");
        text.textContent = message.content ?? "";

        const meta = document.createElement("span");
        meta.classList.add("message__meta");
        meta.textContent = formatClockTime(sentAt);

        bubble.append(author, text, meta);
        wrapper.appendChild(bubble);
        messagesContainer.appendChild(wrapper);
        messagesContainer.scrollTop = messagesContainer.scrollHeight;
    };

    const renderMessages = (messages) => {
        messagesContainer.innerHTML = "";

        if (!messages || messages.length === 0) {
            showEmptyState();
            setComposerState(connectionStarted && Boolean(currentThreadId));
            return;
        }

        messagesContainer.dataset.empty = "false";
        messages.forEach(message => appendMessage(message));
        setComposerState(connectionStarted && Boolean(currentThreadId));
    };

    const joinThread = async (threadId, threadType) => {
        if (!threadId) {
            return;
        }

        const normalisedType = normaliseThreadType(threadType);

        if (!connectionStarted) {
            pendingJoin = { id: threadId, type: normalisedType };
            return;
        }

        const method = normalisedType === THREAD_TYPES.MEETING ? "JoinMeeting" : "JoinFriendship";

        try {
            await connection.invoke(method, threadId);
            pendingJoin = null;
        } catch (error) {
            console.error("Неуспешно присъединяване към разговора:", error);
        }
    };

    const leaveThread = async (threadId, threadType) => {
        if (!threadId || !connectionStarted) {
            return;
        }

        const normalisedType = normaliseThreadType(threadType);
        const method = normalisedType === THREAD_TYPES.MEETING ? "LeaveMeeting" : "LeaveFriendship";

        try {
            await connection.invoke(method, threadId);
        } catch (error) {
            console.warn("Неуспешно напускане на разговора:", error);
        }
    };

    const loadThread = async (button) => {
        if (!button) {
            currentThreadId = null;
            currentThreadType = null;
            applyActiveThread(null);
            return;
        }

        const threadId = button.dataset.threadId;
        const threadType = normaliseThreadType(button.dataset.threadType);

        if (!threadId) {
            return;
        }

        const previousThreadId = currentThreadId;
        const previousThreadType = currentThreadType;
        const previousButton = previousThreadId ? findThreadButton(previousThreadId, previousThreadType) : null;

        currentThreadId = threadId;
        currentThreadType = threadType;

        try {
            const response = await fetch(button.dataset.fetchUrl ?? `/Chat/Thread?id=${threadId}&type=${threadType}`, {
                headers: {
                    "X-Requested-With": "XMLHttpRequest"
                }
            });

            if (!response.ok) {
                throw new Error(`HTTP ${response.status}`);
            }

            const payload = await response.json();
            messagesContainer.dataset.threadId = threadId;
            messagesContainer.dataset.threadType = threadType;

            renderMessages(payload.messages || []);

            if (payload.displayName) {
                button.dataset.displayName = payload.displayName;
            }

            if (payload.secondaryLabel) {
                button.dataset.secondary = payload.secondaryLabel;
            }

            if (payload.lastActivity) {
                button.dataset.status = payload.lastActivity;
            }

            if (Object.prototype.hasOwnProperty.call(payload, "isOnline")) {
                updateThreadOnlineState(button, Boolean(payload.isOnline));
            }

            applyActiveThread(button);

            if (Array.isArray(payload.messages) && payload.messages.length > 0) {
                const lastMessage = payload.messages[payload.messages.length - 1];
                updateThreadPreview(threadId, threadType, lastMessage);
            } else {
                const preview = button.querySelector(".messenger-thread__preview");
                if (preview) {
                    preview.textContent = "Няма съобщения";
                }

                const meta = button.querySelector(".messenger-thread__meta");
                if (meta) {
                    meta.textContent = "";
                }

                button.dataset.lastMessage = "";
            }

            await leaveThread(previousThreadId, previousThreadType);
            await joinThread(threadId, threadType);

            if (messageInput) {
                messageInput.focus();
            }

            await markThreadAsRead(threadId, threadType);
        } catch (error) {
            console.error("Failed to load conversation:", error);
            currentThreadId = previousThreadId;
            currentThreadType = previousThreadType;
            messagesContainer.dataset.threadId = previousThreadId ?? "";
            messagesContainer.dataset.threadType = previousThreadType ?? "";

            const existingAlert = messagesContainer.querySelector(".chat-thread-error");
            if (existingAlert) {
                existingAlert.remove();
            }

            const alert = document.createElement("div");
            alert.className = "alert alert-danger chat-thread-error";
            alert.textContent = "Неуспешно зареждане на разговора. Опитайте отново.";
            messagesContainer.appendChild(alert);

            if (previousButton) {
                applyActiveThread(previousButton);
            } else {
                applyActiveThread(null);
            }
        }
    };

    connection.on("ReceiveMessage", payload => {
        if (!payload) {
            return;
        }

        let threadId = null;
        let threadType = null;

        if (payload.meetingId) {
            threadId = payload.meetingId.toString();
            threadType = THREAD_TYPES.MEETING;
        } else if (payload.friendshipId) {
            threadId = payload.friendshipId.toString();
            threadType = THREAD_TYPES.FRIENDSHIP;
        } else {
            return;
        }

        const message = {
            id: payload.messageId,
            senderId: payload.senderId,
            senderName: payload.senderName,
            content: payload.content,
            sentAt: payload.sentAt,
            friendshipId: payload.friendshipId,
            meetingId: payload.meetingId
        };

        updateThreadPreview(threadId, threadType, message);

        if (threadId !== currentThreadId || threadType !== currentThreadType) {
            return;
        }

        appendMessage(message);

        const senderId = message.senderId?.toString?.() ?? message.senderId ?? '';
        if (senderId && senderId.toString() !== getCurrentUserId()) {
            markThreadAsRead(threadId, threadType);
        }
    });

    connection.on("PresenceChanged", payload => {
        if (!payload) {
            return;
        }

        const rawId = payload.userId ?? payload.UserId;
        if (!rawId) {
            return;
        }

        const friendId = rawId.toString();
        const isOnline = Boolean(payload.isOnline ?? payload.IsOnline);

        document
            .querySelectorAll(`.messenger-thread[data-friend-id="${friendId}"]`)
            .forEach(button => updateThreadOnlineState(button, isOnline));
    });

    connection.onreconnecting(() => {
        connectionStarted = false;
        setComposerState(false);
    });

    connection.onreconnected(async () => {
        connectionStarted = true;
        if (currentThreadId && currentThreadType) {
            await joinThread(currentThreadId, currentThreadType);
        }

        setComposerState(Boolean(currentThreadId));
    });

    connection.onclose(() => {
        connectionStarted = false;
        setComposerState(false);
    });

    connection.start()
        .then(async () => {
            connectionStarted = true;
            if (pendingJoin) {
                await joinThread(pendingJoin.id, pendingJoin.type);
                pendingJoin = null;
            }

            setComposerState(Boolean(currentThreadId));

            if (messageInput && currentThreadId) {
                messageInput.focus();
            }
        })
        .catch(error => {
            console.error("Failed to connect to chat hub:", error);
            const errorAlert = document.createElement("div");
            errorAlert.className = "alert alert-danger";
            errorAlert.textContent = "Неуспешно свързване с чат услугата. Опитайте по-късно.";
            messagesContainer.innerHTML = "";
            messagesContainer.appendChild(errorAlert);
            setComposerState(false);
        });

    const resizeComposer = () => {
        if (!messageInput) {
            return;
        }

        messageInput.style.height = "auto";
        messageInput.style.height = `${Math.min(messageInput.scrollHeight, 220)}px`;
    };

    if (messageInput) {
        messageInput.addEventListener("input", () => {
            resizeComposer();
        });

        messageInput.addEventListener("keydown", event => {
            if (event.key === "Enter" && !event.shiftKey) {
                event.preventDefault();
                chatForm.requestSubmit();
            }
        });

        resizeComposer();
    }

    chatForm.addEventListener("submit", async event => {
        event.preventDefault();

        const message = messageInput?.value.trim();
        if (!message || !currentThreadId || !currentThreadType) {
            return;
        }

        sendButton.disabled = true;

        const method = currentThreadType === THREAD_TYPES.MEETING ? "SendMeetingMessage" : "SendMessage";

        try {
            await connection.invoke(method, currentThreadId, message);
        } catch (error) {
            console.error("Error sending message:", error);
        } finally {
            sendButton.disabled = false;
            if (messageInput) {
                messageInput.value = "";
                resizeComposer();
                messageInput.focus();
            }
        }
    });

    threadButtons().forEach(button => {
        button.addEventListener("click", () => {
            loadThread(button);
        });
    });

    sidebarToggleButtons.forEach(button => {
        button.addEventListener("click", () => {
            const filter = normaliseThreadType(button.dataset.threadFilter);
            applySidebarFilter(filter);
        });
    });

    if (searchInput) {
        searchInput.addEventListener("input", () => {
            const term = searchInput.value.trim().toLocaleLowerCase("bg-BG");
            const buttons = threadButtons();

            buttons.forEach(button => {
                if (!term) {
                    button.classList.remove("is-hidden");
                    return;
                }

                const haystack = `${button.dataset.displayName ?? ""} ${button.dataset.lastMessage ?? ""} ${button.dataset.secondary ?? ""}`
                    .toLocaleLowerCase("bg-BG");
                button.classList.toggle("is-hidden", !haystack.includes(term));
            });

            const visibleButton = buttons.find(btn => !btn.classList.contains("is-hidden"));
            const activeButton = buttons.find(btn => btn.classList.contains("active"));

            if (activeButton && activeButton.classList.contains("is-hidden")) {
                loadThread(visibleButton ?? null);
            }
        });
    }

    if (currentThreadId) {
        const activeButton = findThreadButton(currentThreadId, currentThreadType);
        if (activeButton) {
            loadThread(activeButton);
        } else {
            applyActiveThread(null);
        }
    } else {
        applyActiveThread(null);
    }
})();
