// GEX Visualizer JS Interop Functions

window.GexInterop = {
    toggleFullscreen: function () {
        if (!document.fullscreenElement) {
            document.documentElement.requestFullscreen().catch(err => {
                console.warn('Fullscreen not available:', err.message);
            });
        } else {
            document.exitFullscreen();
        }
    },

    isFullscreen: function () {
        return !!document.fullscreenElement;
    },

    // Axis drag state
    _dragState: null,
    _dotNetRef: null,

    // Initialize drag handling for a chart component
    initAxisDrag: function (dotNetRef) {
        this._dotNetRef = dotNetRef;

        // Global mouse handlers
        document.addEventListener('mousemove', this._onMouseMove.bind(this));
        document.addEventListener('mouseup', this._onMouseUp.bind(this));
        document.addEventListener('touchmove', this._onTouchMove.bind(this), { passive: false });
        document.addEventListener('touchend', this._onMouseUp.bind(this));
    },

    // Clean up drag handling
    disposeAxisDrag: function () {
        document.removeEventListener('mousemove', this._onMouseMove.bind(this));
        document.removeEventListener('mouseup', this._onMouseUp.bind(this));
        document.removeEventListener('touchmove', this._onTouchMove.bind(this));
        document.removeEventListener('touchend', this._onMouseUp.bind(this));
        this._dotNetRef = null;
        this._dragState = null;
    },

    // Start Y-axis drag
    startYAxisDrag: function (clientY, currentScale) {
        this._dragState = {
            axis: 'y',
            startY: clientY,
            startScale: currentScale
        };
    },

    // Start X-axis drag
    startXAxisDrag: function (clientX, currentScale) {
        this._dragState = {
            axis: 'x',
            startX: clientX,
            startScale: currentScale
        };
    },

    // Start chart drag (for price adjustment)
    startChartDrag: function (clientY, currentPrice) {
        this._dragState = {
            axis: 'chart',
            startY: clientY,
            startPrice: currentPrice
        };
    },

    _onMouseMove: function (e) {
        if (!this._dragState || !this._dotNetRef) return;

        if (this._dragState.axis === 'y') {
            const deltaY = this._dragState.startY - e.clientY;
            const scaleDelta = deltaY * 0.005;
            const newScale = Math.max(0.2, Math.min(3.0, this._dragState.startScale + scaleDelta));
            this._dotNetRef.invokeMethodAsync('OnYAxisDrag', newScale);
        }
        else if (this._dragState.axis === 'x') {
            const deltaX = e.clientX - this._dragState.startX;
            const scaleDelta = deltaX * 0.005;
            const newScale = Math.max(0.3, Math.min(3.0, this._dragState.startScale + scaleDelta));
            this._dotNetRef.invokeMethodAsync('OnXAxisDrag', newScale);
        }
        else if (this._dragState.axis === 'chart') {
            const deltaY = this._dragState.startY - e.clientY;
            const priceDelta = deltaY * 0.5;
            this._dotNetRef.invokeMethodAsync('OnChartDrag', this._dragState.startPrice + priceDelta);
        }
    },

    _onTouchMove: function (e) {
        if (!this._dragState) return;
        e.preventDefault();
        const touch = e.touches[0];
        this._onMouseMove({ clientX: touch.clientX, clientY: touch.clientY });
    },

    _onMouseUp: function () {
        if (!this._dragState) return;
        this._dragState = null;
        if (this._dotNetRef) {
            this._dotNetRef.invokeMethodAsync('OnDragEnd');
        }
    },

    // LocalStorage operations
    localStorage: {
        getItem: function (key) {
            return localStorage.getItem(key);
        },

        setItem: function (key, value) {
            localStorage.setItem(key, value);
        },

        removeItem: function (key) {
            localStorage.removeItem(key);
        },

        clear: function () {
            localStorage.clear();
        },

        getKeys: function () {
            const keys = [];
            for (let i = 0; i < localStorage.length; i++) {
                keys.push(localStorage.key(i));
            }
            return keys;
        },

        length: function () {
            return localStorage.length;
        }
    },

    // Download file utility
    downloadFile: function (filename, content, mimeType) {
        const blob = new Blob([content], { type: mimeType });
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = filename;
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
        URL.revokeObjectURL(url);
    },

    // Export chart panel as PNG
    exportChartAsPng: async function (panelSelector, filename) {
        const panel = document.querySelector(panelSelector);
        if (!panel) {
            console.error('Chart panel not found:', panelSelector);
            return false;
        }

        try {
            const ctx = this._createChartContext(panel);
            await this._renderPanelToContext(panel, ctx.context, ctx.width, ctx.height);
            this._downloadCanvas(ctx.canvas, filename);
            return true;
        } catch (err) {
            console.error('Failed to export chart:', err);
            return false;
        }
    },

    // Export both charts as a combined image
    exportBothChartsAsPng: async function (filename) {
        const panels = document.querySelectorAll('.chart-panel');
        if (panels.length < 2) {
            console.error('Both chart panels not found');
            return false;
        }

        try {
            const computedStyle = getComputedStyle(document.documentElement);
            const bgDark = computedStyle.getPropertyValue('--bg-dark').trim() || '#0d1117';
            const textMuted = computedStyle.getPropertyValue('--text-muted').trim() || '#8b949e';

            const rect1 = panels[0].getBoundingClientRect();
            const rect2 = panels[1].getBoundingClientRect();
            const scale = 2;
            const padding = 20;
            const totalWidth = (rect1.width + rect2.width + padding * 3) * scale;
            const totalHeight = (Math.max(rect1.height, rect2.height) + padding * 2 + 30) * scale;

            const canvas = document.createElement('canvas');
            canvas.width = totalWidth;
            canvas.height = totalHeight;
            const ctx = canvas.getContext('2d');
            ctx.scale(scale, scale);

            // Fill background
            ctx.fillStyle = bgDark;
            ctx.fillRect(0, 0, totalWidth / scale, totalHeight / scale);

            // Add title
            ctx.font = 'bold 14px "JetBrains Mono", monospace';
            ctx.fillStyle = textMuted;
            const dateEl = document.querySelector('.year-badge');
            const priceEl = document.querySelector('.metric-value.price-value');
            const title = `GEX Visualizer - ${priceEl?.textContent || ''} - ${dateEl?.textContent || ''}`;
            ctx.fillText(title, padding, padding + 14);

            // Draw each panel
            for (let i = 0; i < 2; i++) {
                const panel = panels[i];
                const pRect = panel.getBoundingClientRect();
                const offsetX = padding + i * (pRect.width + padding);
                const offsetY = padding + 30;

                const tempCanvas = document.createElement('canvas');
                tempCanvas.width = pRect.width * scale;
                tempCanvas.height = pRect.height * scale;
                const tempCtx = tempCanvas.getContext('2d');
                tempCtx.scale(scale, scale);

                await this._renderPanelToContext(panel, tempCtx, pRect.width, pRect.height);
                ctx.drawImage(tempCanvas, offsetX, offsetY, pRect.width, pRect.height);
            }

            this._downloadCanvas(canvas, filename);
            return true;
        } catch (err) {
            console.error('Failed to export charts:', err);
            return false;
        }
    },

    // Create canvas context for a panel
    _createChartContext: function (panel) {
        const rect = panel.getBoundingClientRect();
        const scale = 2;
        const canvas = document.createElement('canvas');
        canvas.width = rect.width * scale;
        canvas.height = rect.height * scale;
        const ctx = canvas.getContext('2d');
        ctx.scale(scale, scale);
        return { canvas, context: ctx, width: rect.width, height: rect.height };
    },

    // Render a single panel to canvas context
    _renderPanelToContext: async function (panel, ctx, width, height) {
        const cs = getComputedStyle(document.documentElement);
        const colors = {
            bgPanel: cs.getPropertyValue('--bg-panel').trim() || '#161b22',
            textPrimary: cs.getPropertyValue('--text-primary').trim() || '#e6edf3',
            textMuted: cs.getPropertyValue('--text-muted').trim() || '#8b949e',
            accentCyan: cs.getPropertyValue('--accent-cyan').trim() || '#00e5ff',
            accentRed: cs.getPropertyValue('--accent-red').trim() || '#ef5350',
            borderColor: cs.getPropertyValue('--border-color').trim() || '#30363d'
        };

        // Background
        ctx.fillStyle = colors.bgPanel;
        ctx.fillRect(0, 0, width, height);

        // Header
        const header = panel.querySelector('.panel-header');
        if (header) {
            const titleEl = header.querySelector('.panel-title');
            const subtitleEl = header.querySelector('.panel-subtitle');
            const statusEl = header.querySelector('.status-value');

            ctx.font = 'bold 14px "JetBrains Mono", monospace';
            ctx.fillStyle = titleEl?.classList.contains('red') ? colors.accentRed : colors.accentCyan;
            ctx.fillText(titleEl?.textContent || '', 16, 24);

            ctx.font = '11px "JetBrains Mono", monospace';
            ctx.fillStyle = colors.textMuted;
            ctx.fillText(subtitleEl?.textContent || '', 16, 40);

            if (statusEl) {
                ctx.font = 'bold 11px "JetBrains Mono", monospace';
                ctx.fillStyle = colors.textPrimary;
                const statusWidth = ctx.measureText(statusEl.textContent).width;
                ctx.fillText(statusEl.textContent || '', width - statusWidth - 16, 32);
            }
        }

        // SVG Chart
        const svg = panel.querySelector('.chart-svg');
        if (svg) {
            await this._renderSvgToContext(ctx, svg, width, height, colors);
        }

        // Y-axis labels
        const yAxis = panel.querySelector('.y-axis');
        if (yAxis) {
            ctx.font = '10px "JetBrains Mono", monospace';
            ctx.fillStyle = colors.textMuted;
            const labels = yAxis.querySelectorAll('.y-label');
            const chartHeight = height - 120;
            labels.forEach((label, i) => {
                const y = 55 + (i * chartHeight / (labels.length - 1)) + 4;
                ctx.fillText(label.textContent || '', 8, y);
            });
        }

        // X-axis labels
        const xAxis = panel.querySelector('.x-axis');
        if (xAxis) {
            ctx.font = '9px "JetBrains Mono", monospace';
            ctx.fillStyle = colors.textMuted;
            const labels = xAxis.querySelectorAll('.x-label');
            const chartWidth = width - 80;
            labels.forEach((label, i) => {
                const x = 60 + (i * chartWidth / (labels.length - 1));
                const textWidth = ctx.measureText(label.textContent).width;
                ctx.fillText(label.textContent || '', x - textWidth / 2, height - 40);
            });
        }

        // Data overlay
        const overlay = panel.querySelector('.data-overlay');
        if (overlay) {
            ctx.font = '10px "JetBrains Mono", monospace';
            const rows = overlay.querySelectorAll('.data-row');
            let y = height - 24;
            rows.forEach(row => {
                const spans = row.querySelectorAll('span');
                if (spans.length >= 2) {
                    ctx.fillStyle = colors.textMuted;
                    ctx.fillText(spans[0].textContent || '', 16, y);
                    const valEl = spans[1];
                    ctx.fillStyle = valEl.classList.contains('cyan') ? colors.accentCyan :
                                   valEl.classList.contains('red') ? colors.accentRed : colors.textPrimary;
                    ctx.fillText(valEl.textContent || '', 120, y);
                }
                y += 14;
            });
        }

        // Border
        ctx.strokeStyle = colors.borderColor;
        ctx.lineWidth = 1;
        ctx.strokeRect(0.5, 0.5, width - 1, height - 1);
    },

    // Render SVG chart to canvas
    _renderSvgToContext: async function (ctx, svg, width, height, colors) {
        const svgClone = svg.cloneNode(true);

        // Inline styles
        svgClone.querySelectorAll('.center-line').forEach(el => {
            el.setAttribute('stroke', colors.borderColor);
            el.setAttribute('stroke-width', '0.5');
            el.setAttribute('stroke-dasharray', '2,2');
        });
        svgClone.querySelectorAll('.zero-gamma-line').forEach(el => {
            el.setAttribute('stroke', colors.accentCyan);
            el.setAttribute('stroke-width', '0.5');
            el.setAttribute('stroke-dasharray', '4,4');
        });
        svgClone.querySelectorAll('.price-line').forEach(el => {
            el.setAttribute('stroke', '#ffffff');
            el.setAttribute('stroke-width', '0.5');
        });
        svgClone.querySelectorAll('.bar-positive').forEach(el => {
            el.setAttribute('fill', colors.accentCyan);
        });
        svgClone.querySelectorAll('.bar-negative').forEach(el => {
            el.setAttribute('fill', colors.accentRed);
        });
        svgClone.querySelectorAll('.bar-saturated').forEach(el => {
            el.setAttribute('fill', '#ff9800');
        });
        svgClone.querySelectorAll('.zero-marker, .price-marker').forEach(el => {
            el.setAttribute('fill', colors.textMuted);
            el.setAttribute('font-size', '3');
            el.setAttribute('font-family', 'JetBrains Mono, monospace');
        });

        svgClone.setAttribute('width', width - 80);
        svgClone.setAttribute('height', height - 120);
        svgClone.setAttribute('xmlns', 'http://www.w3.org/2000/svg');

        const svgData = new XMLSerializer().serializeToString(svgClone);
        const svgBlob = new Blob([svgData], { type: 'image/svg+xml;charset=utf-8' });
        const svgUrl = URL.createObjectURL(svgBlob);

        const img = new Image();
        await new Promise((resolve, reject) => {
            img.onload = resolve;
            img.onerror = reject;
            img.src = svgUrl;
        });

        ctx.drawImage(img, 60, 55, width - 80, height - 120);
        URL.revokeObjectURL(svgUrl);
    },

    // Download canvas as PNG
    _downloadCanvas: function (canvas, filename) {
        canvas.toBlob(blob => {
            if (blob) {
                const url = URL.createObjectURL(blob);
                const a = document.createElement('a');
                a.href = url;
                a.download = filename;
                document.body.appendChild(a);
                a.click();
                document.body.removeChild(a);
                URL.revokeObjectURL(url);
            }
        }, 'image/png');
    }
};
