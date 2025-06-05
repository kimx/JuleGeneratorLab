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

async function createAndDownloadZip(files, zipFileName) {
    if (typeof JSZip === 'undefined') {
        console.error('JSZip library is not loaded.');
        alert('Error: JSZip library not found. Cannot create ZIP file.');
        return;
    }
    if (!files || files.length === 0) {
        alert('No files to zip.');
        return;
    }

    var zip = new JSZip();
    files.forEach(function(file) {
        zip.file(file.name, file.content);
    });

    try {
        const zipBlob = await zip.generateAsync({ type: "blob" });
        // Use the existing saveAsFile logic, or a simplified version for blobs
        // Assuming saveAsFile is globally available (it's defined in App.razor inline script)
        if (typeof saveAsFile === 'function') {
             // Convert blob to data URL for saveAsFile, or adapt saveAsFile to handle blobs
            const dataUrl = URL.createObjectURL(zipBlob);
            saveAsFile(zipFileName, dataUrl); // saveAsFile expects a data URL
            URL.revokeObjectURL(dataUrl); // Clean up the object URL
        } else {
            // Fallback if saveAsFile is not available from App.razor for some reason
            var link = document.createElement('a');
            link.href = URL.createObjectURL(zipBlob);
            link.download = zipFileName;
            document.body.appendChild(link);
            link.click();
            document.body.removeChild(link);
            URL.revokeObjectURL(link.href); // Clean up
            console.warn('saveAsFile function not found, using direct download for ZIP.');
        }
    } catch (e) {
        console.error("Error creating zip file: ", e);
        alert("Error creating zip file: " + e.message);
    }
}
