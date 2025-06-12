(function () {
    const MembershipFormEmbed = {
        init: function (targetId = 'application-form') {
            const container = document.getElementById(targetId);
            if (!container) {
                console.warn(`[MembershipForm] No element with ID "${targetId}" found.`);
                return;
            }

            // Base URL for the form
            const formUrlBase = window.location.origin + '/Membership/Apply';
            // Build a URL object so we can manipulate the search params safely
            const url = new URL(formUrlBase, window.location.origin);

            // Copy any existing params from the current page
            const currentParams = new URLSearchParams(window.location.search);
            currentParams.forEach((value, key) => {
                url.searchParams.set(key, value);
            });

            // Append/overwrite our suppressLogo flag
            url.searchParams.set('supressLogo', 'true');

            // Create and inject the iframe
            const iframe = document.createElement('iframe');
            iframe.src = url.href;
            iframe.style.width = '100%';
            iframe.style.border = 'none';
            iframe.setAttribute('scrolling', 'no');
            iframe.setAttribute('title', 'Membership Application Form');
            iframe.style.minHeight = '800px';
            container.appendChild(iframe);

            // Listen for height messages from the iframe
            window.addEventListener('message', function (event) {
                if (event.origin !== window.location.origin) return;
                if (event.data && event.data.frameHeight) {
                    iframe.style.height = event.data.frameHeight + 'px';
                }
            });
        }
    };

    window.MembershipFormEmbed = MembershipFormEmbed;
})();
