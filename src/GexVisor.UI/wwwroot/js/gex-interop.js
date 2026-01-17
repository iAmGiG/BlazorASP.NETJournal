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
    }
};
