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
    }
};
