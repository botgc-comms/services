(function () {
    const MembershipFormEmbed = {
        init: function (targetId = 'application-form') {
            const container = document.getElementById(targetId);

            if (!container) {
                console.warn(`[MembershipForm] No element with ID "${targetId}" found.`);
                return;
            }

            // Dynamically determine iframe URL
            const queryString = window.location.search;
            const formUrlBase = window.location.origin + '/Membership/Apply';  // Relative path
            const fullUrl = formUrlBase + queryString;

            // Create iframe
            const iframe = document.createElement('iframe');
            iframe.src = fullUrl;
            iframe.style.width = '100%';
            iframe.style.border = 'none';
            iframe.setAttribute('scrolling', 'no');
            iframe.setAttribute('title', 'Membership Application Form');

            // Set an initial min height
            iframe.style.minHeight = '800px';

            // Inject the iframe into the container
            container.appendChild(iframe);

            // Listen for messages from the iframe to resize it
            window.addEventListener('message', function (event) {
                // 🔐 Only trust messages from same origin
                if (event.origin !== window.location.origin) return;
                if (event.data && event.data.frameHeight) {
                    iframe.style.height = event.data.frameHeight + 'px';
                }
            });
        }
    };

    window.MembershipFormEmbed = MembershipFormEmbed;
})();
