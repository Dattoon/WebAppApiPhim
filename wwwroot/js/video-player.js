class VideoPlayer {
    constructor(options) {
        this.videoContainer = document.getElementById(options.containerId);
        this.videoElement = document.getElementById(options.videoId);
        this.userId = options.userId;
        this.movieSlug = options.movieSlug;
        this.movieName = options.movieName;
        this.updateProgressUrl = options.updateProgressUrl;

        this.progressInterval = null;
        this.currentProgress = 0;

        this.initPlayer();
    }

    initPlayer() {
        if (!this.videoElement) return;

        // Add event listeners
        this.videoElement.addEventListener('play', () => this.startTrackingProgress());
        this.videoElement.addEventListener('pause', () => this.stopTrackingProgress());
        this.videoElement.addEventListener('ended', () => this.handleVideoEnded());

        // If there's saved progress, seek to that position
        if (this.currentProgress > 0 && this.videoElement.duration) {
            const seekPosition = (this.currentProgress / 100) * this.videoElement.duration;
            if (seekPosition > 0 && seekPosition < this.videoElement.duration - 10) {
                this.videoElement.currentTime = seekPosition;
            }
        }
    }

    startTrackingProgress() {
        this.stopTrackingProgress();
        this.progressInterval = setInterval(() => this.updateProgress(), 5000); // Update every 5 seconds
    }

    stopTrackingProgress() {
        if (this.progressInterval) {
            clearInterval(this.progressInterval);
            this.progressInterval = null;
        }
    }

    updateProgress() {
        if (!this.videoElement || !this.videoElement.duration) return;

        const progress = (this.videoElement.currentTime / this.videoElement.duration) * 100;
        this.currentProgress = progress;

        // Save progress to server if user is logged in
        if (this.userId) {
            this.saveProgressToServer(progress);
        }
    }

    saveProgressToServer(progress) {
        fetch(this.updateProgressUrl, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value
            },
            body: JSON.stringify({
                slug: this.movieSlug,
                name: this.movieName,
                percentage: progress
            })
        }).catch(error => console.error('Error saving progress:', error));
    }

    handleVideoEnded() {
        this.stopTrackingProgress();
        this.saveProgressToServer(100);
    }

    loadVideo(url, slug) {
        this.stopTrackingProgress();
        this.movieSlug = slug;
        this.videoElement.src = url;
        this.videoElement.load();
        this.videoContainer.style.display = 'block';

        // Scroll to video
        this.videoContainer.scrollIntoView({ behavior: 'smooth', block: 'start' });
    }
}