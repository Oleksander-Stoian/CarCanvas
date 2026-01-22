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
        
        // Optimization for massive amount of markers:
        // Use ImageData if points > 5000, otherwise use Path (circles)
        // Circles are nicer but slower. ImageData is instant but just pixels.
        
        const count = pointsFlat.length / 2;
        
        if (count > 5000) {
             // 1. Pixel-based rendering (ImageData) - Extremely fast for 100k+
             const imgData = this.ctx.getImageData(0, 0, this.canvas.width, this.canvas.height);
             const data = imgData.data;
             const w = this.canvas.width;
             const h = this.canvas.height;
             
             // Red color: R=255, G=0, B=0, A=255
             for (let i = 0; i < pointsFlat.length; i+=2) {
                 const x = pointsFlat[i];
                 const y = pointsFlat[i+1];
                 
                 if (x >= 0 && x < w && y >= 0 && y < h) {
                     // Draw a small 3x3 cross or block to make it visible
                     // Center
                     let idx = (y * w + x) * 4;
                     data[idx] = 255; data[idx+1] = 0; data[idx+2] = 0; data[idx+3] = 255;
                     
                     // Plus shape neighbors (optional, makes it thicker than 1px)
                     if (x+1 < w) { idx = (y * w + (x+1)) * 4; data[idx] = 255; data[idx+3] = 255; }
                     if (x-1 >= 0) { idx = (y * w + (x-1)) * 4; data[idx] = 255; data[idx+3] = 255; }
                     if (y+1 < h) { idx = ((y+1) * w + x) * 4; data[idx] = 255; data[idx+3] = 255; }
                     if (y-1 >= 0) { idx = ((y-1) * w + x) * 4; data[idx] = 255; data[idx+3] = 255; }
                 }
             }
             this.ctx.putImageData(imgData, 0, 0);
        } else {
             // 2. Vector-based rendering (Circles) - Nicer for small amounts
            this.ctx.fillStyle = 'red';
            this.ctx.beginPath();
            for (let i = 0; i < pointsFlat.length; i+=2) {
                const x = pointsFlat[i];
                const y = pointsFlat[i+1];
                this.ctx.moveTo(x + 3, y);
                this.ctx.arc(x, y, 3, 0, 2 * Math.PI);
            }
            this.ctx.fill();
        }
    },

    drawRect: function (x, y, w, h, color) {
        if (!this.ctx) return;
        this.ctx.strokeStyle = color;
        this.ctx.lineWidth = 1;
        this.ctx.strokeRect(x, y, w, h);
    }
};
