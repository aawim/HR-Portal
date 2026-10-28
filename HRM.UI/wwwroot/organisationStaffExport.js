export async function download(filename, contentType, streamReference) {
    const data = await streamReference.arrayBuffer();
    const url = URL.createObjectURL(new Blob([data], { type: contentType }));
    const link = document.createElement('a');
    link.href = url;
    link.download = filename;
    document.body.appendChild(link);
    link.click();
    link.remove();
    setTimeout(() => URL.revokeObjectURL(url), 60000);
}
