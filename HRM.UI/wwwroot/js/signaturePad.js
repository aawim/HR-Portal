window.hrmSignaturePad = (function () {
    const pads = new Map();

    function init(canvasId) {
        const canvas = document.getElementById(canvasId);
        if (!canvas) return false;
        const ctx = canvas.getContext('2d');
        const ratio = Math.max(window.devicePixelRatio || 1, 1);
        const rect = canvas.getBoundingClientRect();
        const width = rect.width > 0 ? rect.width : 420;
        const height = rect.height > 0 ? rect.height : 150;
        canvas.width = Math.round(width * ratio);
        canvas.height = Math.round(height * ratio);
        ctx.scale(ratio, ratio);
        ctx.lineWidth = 2.2;
        ctx.lineCap = 'round';
        ctx.lineJoin = 'round';
        ctx.strokeStyle = '#0f172a';

        let drawing = false;
        let hasInk = false;

        function getPos(e) {
            const r = canvas.getBoundingClientRect();
            const clientX = e.touches ? e.touches[0].clientX : e.clientX;
            const clientY = e.touches ? e.touches[0].clientY : e.clientY;
            return { x: clientX - r.left, y: clientY - r.top };
        }

        function start(e) {
            e.preventDefault();
            drawing = true;
            const pos = getPos(e);
            ctx.beginPath();
            ctx.moveTo(pos.x, pos.y);
        }

        function move(e) {
            if (!drawing) return;
            e.preventDefault();
            const pos = getPos(e);
            ctx.lineTo(pos.x, pos.y);
            ctx.stroke();
            hasInk = true;
        }

        function stop() {
            if (!drawing) return;
            drawing = false;
            ctx.closePath();
        }

        canvas.addEventListener('mousedown', start);
        canvas.addEventListener('mousemove', move);
        window.addEventListener('mouseup', stop);
        canvas.addEventListener('touchstart', start, { passive: false });
        canvas.addEventListener('touchmove', move, { passive: false });
        canvas.addEventListener('touchend', stop);

        pads.set(canvasId, {
            clear: () => {
                ctx.clearRect(0, 0, canvas.width, canvas.height);
                hasInk = false;
            },
            isEmpty: () => !hasInk,
            toDataUrl: () => hasInk ? canvas.toDataURL('image/png') : ''
        });
        return true;
    }

    return {
        init,
        clear: (id) => pads.get(id)?.clear(),
        isEmpty: (id) => pads.get(id)?.isEmpty() ?? true,
        toDataUrl: (id) => pads.get(id)?.toDataUrl() ?? ''
    };
})();
