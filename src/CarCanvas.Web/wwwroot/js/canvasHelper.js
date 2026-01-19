window.canvasHelper = {
    canvas: null,
    ctx: null,

    init: function (canvasId) {
        this.canvas = document.getElementById(canvasId);
        if (this.canvas) {
            this.ctx = this.canvas.getContext('2d');
        }
    },

    clear: function () {
        if (this.ctx && this.canvas) {
            this.ctx.clearRect(0, 0, this.canvas.width, this.canvas.height);
        }
    },

    drawPoints: function (color, pointsChunk) {
        if (!this.ctx) return;
        this.ctx.fillStyle = color;
        // Optimization: use 1x1 rects or putImageData if points are dense
        // For simplicity, fillRect 1x1
        for (let i = 0; i < pointsChunk.length; i+=2) {
            // pointsChunk is flat array [x, y, x, y...]
            this.ctx.fillRect(pointsChunk[i], pointsChunk[i+1], 1, 1);
        }
    },

    drawLine: function (x1, y1, x2, y2) {
        if (!this.ctx) return;
        this.ctx.strokeStyle = '#000000'; // Default black for lines
        this.ctx.beginPath();
        this.ctx.moveTo(x1, y1);
        this.ctx.lineTo(x2, y2);
        this.ctx.stroke();
    },
    
    drawLinesBatch: function (linesFlat) {
        if (!this.ctx) return;
        this.ctx.strokeStyle = '#000000';
        this.ctx.beginPath();
        for (let i = 0; i < linesFlat.length; i+=4) {
            this.ctx.moveTo(linesFlat[i], linesFlat[i+1]);
            this.ctx.lineTo(linesFlat[i+2], linesFlat[i+3]);
        }
        this.ctx.stroke();
    },

    drawMarker: function (x, y) {
        if (!this.ctx) return;
        this.ctx.fillStyle = 'red';
        this.ctx.beginPath();
        this.ctx.arc(x, y, 3, 0, 2 * Math.PI);
        this.ctx.fill();
    },
    
    drawMarkersBatch: function (pointsFlat) {
        if (!this.ctx) return;
        this.ctx.fillStyle = 'red';
        for (let i = 0; i < pointsFlat.length; i+=2) {
            this.ctx.beginPath();
            this.ctx.arc(pointsFlat[i], pointsFlat[i+1], 3, 0, 2 * Math.PI);
            this.ctx.fill();
        }
    }
};
