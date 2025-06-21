window.initScrollAnimations = function() {
    // Create intersection observer for scroll animations
    const observerOptions = {
        root: null,
        rootMargin: '0px 0px -10% 0px',
        threshold: 0.1
    };

    const scrollObserver = new IntersectionObserver((entries) => {
        entries.forEach(entry => {
            if (entry.isIntersecting) {
                entry.target.classList.add('visible');
                // Once visible, stop observing to prevent re-triggering
                scrollObserver.unobserve(entry.target);
            }
        });
    }, observerOptions);

    // Observe all scroll items
    const scrollItems = document.querySelectorAll('.scroll-item');
    scrollItems.forEach(item => {
        scrollObserver.observe(item);
    });

    // Also check if any items are already in view on load
    scrollItems.forEach(item => {
        const rect = item.getBoundingClientRect();
        const inView = rect.top < window.innerHeight && rect.bottom > 0;
        if (inView) {
            item.classList.add('visible');
            scrollObserver.unobserve(item);
        }
    });

    // Cleanup function
    window.cleanupScrollAnimations = function() {
        if (scrollObserver) {
            scrollObserver.disconnect();
        }
    };
};