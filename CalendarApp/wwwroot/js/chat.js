(() => {
    const messagesContainer = document.getElementById("chatMessages");
    const messageInput = document.getElementById("messageInput");
    const userInput = document.getElementById("userInput");
    const sendButton = document.getElementById("sendButton");
    const chatForm = document.getElementById("chatForm");
    const chatThreads = Array.from(document.querySelectorAll(".messenger-thread"));
    const searchInput = document.getElementById("chatSearch");
    const activeContactName = document.getElementById("activeContactName");
    const activeContactStatus = document.getElementById("activeContactStatus");
    const activeContactAvatar = document.getElementById("activeContactAvatar");

    if (!messagesContainer || !messageInput || !userInput || !sendButton || !chatForm || !window.signalR) {
        return;
    }

    const connection = new signalR.HubConnectionBuilder()
        .withUrl("/hubs/chat")
        .withAutomaticReconnect()
        .build();

    const accentClasses = new Set([
        "accent-blue",
        "accent-purple",
        "accent-green",
        "accent-orange",
        "accent-teal"
    ]);

    const normalizedUser = () => (userInput.value || "").trim() || "Anonymous";

    const ensureActiveContact = (button) => {
        if (!button) {
            return;
        }

        chatThreads.forEach(item => item.classList.remove("active"));
        button.classList.add("active");

        const name = button.dataset.contact || "Разговор";
        const status = button.dataset.status || "";
        const initials = button.dataset.avatar || name.substring(0, 2).toUpperCase();
        const accent = button.dataset.accent;

        if (activeContactName) {
            activeContactName.textContent = name;
        }

        if (activeContactStatus) {
            activeContactStatus.textContent = status;
            activeContactStatus.classList.toggle("text-muted", !status);
        }

        if (activeContactAvatar) {
            activeContactAvatar.textContent = initials;

            accentClasses.forEach(cls => activeContactAvatar.classList.remove(cls));

            if (accent && accentClasses.has(accent)) {
                activeContactAvatar.classList.add(accent);
            } else {
                activeContactAvatar.classList.add("accent-blue");
            }
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

    const appendMessage = (user, message) => {
        removeEmptyState();

        const authorName = user?.trim() || "Anonymous";
        const currentUser = normalizedUser().toLocaleLowerCase("bg-BG");
        const isOwnMessage = authorName.toLocaleLowerCase("bg-BG") === currentUser;

        const messageWrapper = document.createElement("div");
        messageWrapper.classList.add("message", isOwnMessage ? "message--outgoing" : "message--incoming");

        const bubble = document.createElement("div");
        bubble.classList.add("message__bubble");

        const author = document.createElement("span");
        author.classList.add("message__author");
        author.textContent = isOwnMessage ? "Вие" : authorName;

        const text = document.createElement("p");
        text.classList.add("message__text");
        text.textContent = message;

        const meta = document.createElement("span");
        meta.classList.add("message__meta");
        meta.textContent = new Intl.DateTimeFormat("bg-BG", {
            hour: "2-digit",
            minute: "2-digit"
        }).format(new Date());

        bubble.append(author, text, meta);
        messageWrapper.appendChild(bubble);
        messagesContainer.appendChild(messageWrapper);
        messagesContainer.scrollTop = messagesContainer.scrollHeight;
    };

    connection.on("ReceiveMessage", (user, message) => {
        appendMessage(user, message);
    });

    connection.start()
        .then(() => {
            sendButton.disabled = false;
            messageInput.focus();
        })
        .catch(error => {
            console.error("Failed to connect to chat hub:", error);
            const errorAlert = document.createElement("div");
            errorAlert.className = "alert alert-danger";
            errorAlert.textContent = "Unable to connect to the live chat service. Please try again later.";
            messagesContainer.innerHTML = "";
            messagesContainer.appendChild(errorAlert);
        });

    const resizeComposer = () => {
        messageInput.style.height = "auto";
        messageInput.style.height = `${Math.min(messageInput.scrollHeight, 220)}px`;
    };

    messageInput.addEventListener("input", resizeComposer);
    resizeComposer();

    messageInput.addEventListener("keydown", event => {
        if (event.key === "Enter" && !event.shiftKey) {
            event.preventDefault();
            chatForm.requestSubmit();
        }
    });

    chatThreads.forEach(button => {
        button.addEventListener("click", () => ensureActiveContact(button));
    });

    ensureActiveContact(chatThreads.find(button => !button.classList.contains("is-hidden")) || chatThreads[0]);

    if (searchInput) {
        searchInput.addEventListener("input", () => {
            const term = searchInput.value.trim().toLocaleLowerCase("bg-BG");

            chatThreads.forEach(button => {
                if (!term) {
                    button.classList.remove("is-hidden");
                    return;
                }

                const haystack = `${button.dataset.contact ?? ""} ${button.dataset.lastMessage ?? ""}`
                    .toLocaleLowerCase("bg-BG");
                button.classList.toggle("is-hidden", !haystack.includes(term));
            });

            const activeButton = chatThreads.find(button => button.classList.contains("active"));
            if (activeButton && activeButton.classList.contains("is-hidden")) {
                ensureActiveContact(chatThreads.find(button => !button.classList.contains("is-hidden")) || null);
            }
        });
    }

    chatForm.addEventListener("submit", event => {
        event.preventDefault();

        const user = normalizedUser();
        const message = messageInput.value.trim();

        if (!message) {
            return;
        }

        sendButton.disabled = true;

        connection.invoke("SendMessage", user, message)
            .catch(error => console.error("Error sending message:", error))
            .finally(() => {
                sendButton.disabled = false;
                messageInput.value = "";
                messageInput.focus();
                resizeComposer();
            });
    });
})();
