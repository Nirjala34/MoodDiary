window.richTextEditor = {
    executeCommand: function (command, value = null) {
        if (command === "removeFormat") {
            document.execCommand("removeFormat", false, null);
            document.execCommand("formatBlock", false, "p");
            return;
        }

        if (value !== null) {
            document.execCommand(command, false, value);
        } else {
            document.execCommand(command, false, null);
        }
    },

    getContent: function (editorId) {
        const el = document.getElementById(editorId);
        return el ? el.innerHTML : "";
    },

    setContent: function (editorId, content) {
        const el = document.getElementById(editorId);
        if (el) el.innerHTML = content ?? "";
    }
};
