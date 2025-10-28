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

    const accentClasses = new Set([
        "accent-blue",
        "accent-purple",
        "accent-green",
        "accent-orange",
        "accent-teal"
    ]);

    const getCurrentUserId = () => messagesContainer.dataset.currentUserId ?? "";

    const chatThreads = () => Array.from(document.querySelectorAll(".messenger-thread"));

    const connection = new signalR.HubConnectionBuilder()
        .withUrl("/hubs/chat")
        .withAutomaticReconnect()
        .build();

    let currentFriendshipId = messagesContainer.dataset.friendshipId || null;
    let connectionStarted = false;
    let pendingJoin = currentFriendshipId;

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

    const updateThreadPreview = (friendshipId, message) => {
        const button = chatThreads().find(item => item.dataset.friendshipId === friendshipId);
        if (!button) {
            return;
        }

        const preview = button.querySelector(".messenger-thread__preview");
        if (preview) {
            preview.textContent = message.content || "";
        }

        const meta = button.querySelector(".messenger-thread__meta");
        if (meta) {
            const sentAt = message.sentAt ? new Date(message.sentAt) : new Date();
            meta.textContent = formatRelativeTime(sentAt);
        }

        button.dataset.lastMessage = message.content || "";
        button.dataset.status = button.dataset.status || "";
    };

    const ensureActiveContact = (button) => {
        chatThreads().forEach(item => item.classList.remove("active"));

        if (!button) {
            if (activeContactName) {
                activeContactName.textContent = "Изберете приятел";
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

            messagesContainer.dataset.friendshipId = "";
            setComposerState(false);
            return;
        }

        button.classList.add("active");

        const name = button.dataset.contact || "Разговор";
        const email = button.dataset.email || "";
        const status = button.dataset.status || "";
        const initials = button.dataset.avatar || name.substring(0, 2).toUpperCase();
        const accent = button.dataset.accent;

        if (activeContactName) {
            activeContactName.textContent = name;
        }

        if (activeContactStatus) {
            const statusText = email || status;
            activeContactStatus.textContent = statusText;
            activeContactStatus.classList.toggle("text-muted", !statusText);
        }

        if (activeContactAvatar) {
            activeContactAvatar.textContent = initials;
            accentClasses.forEach(cls => activeContactAvatar.classList.remove(cls));
            activeContactAvatar.classList.add(accent && accentClasses.has(accent) ? accent : "accent-blue");
        }

        messagesContainer.dataset.friendshipId = button.dataset.friendshipId || "";
        setComposerState(connectionStarted && Boolean(button.dataset.friendshipId));
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
            setComposerState(connectionStarted && Boolean(currentFriendshipId));
            return;
        }

        messagesContainer.dataset.empty = "false";
        messages.forEach(message => appendMessage(message));
        setComposerState(connectionStarted && Boolean(currentFriendshipId));
    };

    const joinFriendship = async (friendshipId) => {
        if (!friendshipId) {
            return;
        }

        if (!connectionStarted) {
            pendingJoin = friendshipId;
            return;
        }

        try {
            await connection.invoke("JoinFriendship", friendshipId);
        } catch (error) {
            console.error("Неуспешно присъединяване към разговора:", error);
        }
    };

    const leaveFriendship = async (friendshipId) => {
        if (!friendshipId || !connectionStarted) {
            return;
        }

        try {
            await connection.invoke("LeaveFriendship", friendshipId);
        } catch (error) {
            console.warn("Неуспешно напускане на разговора:", error);
        }
    };

    const loadThread = async (button) => {
        if (!button) {
            ensureActiveContact(null);
            return;
        }

        const friendshipId = button.dataset.friendshipId;
        if (!friendshipId) {
            return;
        }

        const previousFriendshipId = currentFriendshipId;
        currentFriendshipId = friendshipId;

        try {
            const response = await fetch(button.dataset.fetchUrl ?? `/Chat/Thread?friendshipId=${friendshipId}`, {
                headers: {
                    "X-Requested-With": "XMLHttpRequest"
                }
            });

            if (!response.ok) {
                throw new Error(`HTTP ${response.status}`);
            }

            const payload = await response.json();
            messagesContainer.dataset.friendshipId = friendshipId;

            renderMessages(payload.messages || []);
            ensureActiveContact(button);

            if (payload.friendEmail) {
                button.dataset.email = payload.friendEmail;
            }

            if (payload.lastActivity) {
                button.dataset.status = payload.lastActivity;
            }

            if (Array.isArray(payload.messages) && payload.messages.length > 0) {
                const lastMessage = payload.messages[payload.messages.length - 1];
                updateThreadPreview(friendshipId, lastMessage);
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

            await leaveFriendship(previousFriendshipId);
            await joinFriendship(friendshipId);

            if (messageInput) {
                messageInput.focus();
            }
        } catch (error) {
            console.error("Failed to load conversation:", error);
            currentFriendshipId = previousFriendshipId;
            const existingAlert = messagesContainer.querySelector(".chat-thread-error");
            if (existingAlert) {
                existingAlert.remove();
            }

            const alert = document.createElement("div");
            alert.className = "alert alert-danger chat-thread-error";
            alert.textContent = "Неуспешно зареждане на разговора. Опитайте отново.";
            messagesContainer.appendChild(alert);
        }
    };

    connection.on("ReceiveMessage", payload => {
        if (!payload || !payload.friendshipId) {
            return;
        }

        const friendshipId = payload.friendshipId.toString();
        const message = {
            id: payload.messageId,
            senderId: payload.senderId,
            senderName: payload.senderName,
            content: payload.content,
            sentAt: payload.sentAt
        };

        updateThreadPreview(friendshipId, message);

        if (friendshipId !== currentFriendshipId) {
            return;
        }

        appendMessage(message);
    });

    connection.onreconnecting(() => {
        connectionStarted = false;
        setComposerState(false);
    });

    connection.onreconnected(async () => {
        connectionStarted = true;
        if (currentFriendshipId) {
            await joinFriendship(currentFriendshipId);
        }

        setComposerState(Boolean(currentFriendshipId));
    });

    connection.onclose(() => {
        connectionStarted = false;
        setComposerState(false);
    });

    connection.start()
        .then(async () => {
            connectionStarted = true;
            if (pendingJoin) {
                await joinFriendship(pendingJoin);
                pendingJoin = null;
            }

            setComposerState(Boolean(currentFriendshipId));

            if (messageInput && currentFriendshipId) {
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
        if (!message || !currentFriendshipId) {
            return;
        }

        sendButton.disabled = true;

        try {
            await connection.invoke("SendMessage", currentFriendshipId, message);
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

    chatThreads().forEach(button => {
        button.addEventListener("click", () => {
            loadThread(button);
        });
    });

    if (searchInput) {
        searchInput.addEventListener("input", () => {
            const term = searchInput.value.trim().toLocaleLowerCase("bg-BG");
            const buttons = chatThreads();

            buttons.forEach(button => {
                if (!term) {
                    button.classList.remove("is-hidden");
                    return;
                }

                const haystack = `${button.dataset.contact ?? ""} ${button.dataset.lastMessage ?? ""}`
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

    if (currentFriendshipId) {
        const activeButton = chatThreads().find(button => button.dataset.friendshipId === currentFriendshipId);
        ensureActiveContact(activeButton ?? null);
    } else {
        ensureActiveContact(null);
    }
})();
