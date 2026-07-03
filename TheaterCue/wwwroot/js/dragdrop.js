window.dragDrop = {
    init: function (dotNetRef) {
        let dragging = null;
        let dragId = null;
        let ghost = null;

        // ── Drag de tarjetas ─────────────────────────────────────────
        document.addEventListener('mousedown', e => {
            const handle = e.target.closest('.cue-card__drag-handle');
            const isCtrlClick = e.button === 0 && e.ctrlKey;
            let card = null;

            if (handle) {
                card = handle.closest('.cue-card-wrapper');
            } else if (isCtrlClick) {
                card = e.target.closest('.cue-card-wrapper');
            }

            if (!card) return;

            dragging = card;
            dragId = card.dataset.trackId;
            card.classList.add('dragging');

            // Crear carta fantasma
            ghost = card.cloneNode(true);
            ghost.classList.add('drag-ghost');
            ghost.style.width = card.offsetWidth + 'px';
            ghost.style.height = card.offsetHeight + 'px';
            ghost.style.left = e.clientX + 'px';
            ghost.style.top = e.clientY + 'px';
            document.body.appendChild(ghost);

            e.preventDefault();
        });

        document.addEventListener('mousemove', e => {
            if (!ghost) return;
            ghost.style.left = (e.clientX + 12) + 'px';
            ghost.style.top = (e.clientY + 12) + 'px';
        });

        document.addEventListener('mouseup', e => {
            if (!dragging) return;

            const target = e.target.closest('.cue-card-wrapper');
            const column = e.target.closest('.cue-column');

            if (target && target !== dragging) {
                dotNetRef.invokeMethodAsync('DropOnTrack', dragId, target.dataset.trackId);
            } else if (column && !target) {
                const colIndex = parseInt(column.dataset.columnIndex);
                dotNetRef.invokeMethodAsync('DropOnColumn', dragId, colIndex);
            }

            dragging.classList.remove('dragging');
            dragging = null;
            dragId = null;

            if (ghost) { ghost.remove(); ghost = null; }
        });

        // ── Splitter redimensionable ─────────────────────────────────
        const splitter = document.querySelector('.cue-splitter');
        const workspace = document.querySelector('.cue-workspace');
        if (!splitter || !workspace) return;

        let isResizing = false;

        splitter.addEventListener('mousedown', e => {
            isResizing = true;
            document.body.style.cursor = 'col-resize';
            document.body.style.userSelect = 'none';
            e.preventDefault();
        });

        document.addEventListener('mousemove', e => {
            if (!isResizing) return;
            const rect = workspace.getBoundingClientRect();
            const offset = e.clientX - rect.left;
            const total = rect.width;
            const percent = Math.min(Math.max(offset / total * 100, 20), 80);

            const cols = workspace.querySelectorAll('.cue-column');
            if (cols.length >= 2) {
                cols[0].style.flex = `0 0 ${percent}%`;
                cols[1].style.flex = `0 0 ${100 - percent}%`;
            }
        });

        document.addEventListener('mouseup', () => {
            if (!isResizing) return;
            isResizing = false;
            document.body.style.cursor = '';
            document.body.style.userSelect = '';
        });
    }
};