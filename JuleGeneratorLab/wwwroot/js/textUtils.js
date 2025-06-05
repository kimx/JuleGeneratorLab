window.textUtils = {
    insertTextAtCursor: function (textAreaId, textToInsert) {
        const textArea = document.getElementById(textAreaId);
        if (!textArea) {
            console.error('Text area with id ' + textAreaId + ' not found.');
            return;
        }

        const startPos = textArea.selectionStart;
        const endPos = textArea.selectionEnd;
        const originalValue = textArea.value;

        textArea.value = originalValue.substring(0, startPos) + textToInsert + originalValue.substring(endPos);

        // Move cursor to after inserted text
        textArea.selectionStart = textArea.selectionEnd = startPos + textToInsert.length;

        // Notify Blazor of the change
        var event = new Event('input', { bubbles: true });
        textArea.dispatchEvent(event);
        textArea.focus(); // Keep focus on the textarea
    }
};
