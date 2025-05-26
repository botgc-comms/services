function updateMembershipDescription() {
    const selectedValue = $('#MembershipCategory').val();
    const selected = membershipCategories.find(c => c.name === selectedValue);
    const $desc = $('#membership-description');

    let description = selected?.description || '';

    $('#finance-section').toggle(!!selected?.financeAvailable);

    const hasReferrer = $('#ReferrerId').val();
    if (hasReferrer) {
        if (selected?.referrerEligable) {
            description += ' <strong>Great news – this category qualifies for our referral reward scheme.</strong>';
        } else {
            description += ' <strong>Note: This category is not eligible for the referral reward scheme.</strong>';
        }
    }

    $desc.html(description).toggle(!!selected);
}

async function enableAutocomplete(apiKey) {
    await getAddress.autocomplete('AddressLine1', apiKey, {
        selected: (address) => {
            const line1 = address.formatted_address[0] || ''
            const line2 = address.formatted_address[1] || ''
            const townRaw = address.formatted_address[3] || ''
            const county = address.formatted_address[4] || ''
            const postcode = address.postcode || ''

            // Clean and assign line1
            document.getElementById('AddressLine1').value = line1

            // Conditional adjustment for line2 and town
            if (townRaw.includes(',') && !line2) {
                const [before, after] = townRaw.split(',').map(s => s.trim())
                document.getElementById('AddressLine2').value = before
                document.getElementById('Town').value = after
            } else {
                document.getElementById('AddressLine2').value = line2
                document.getElementById('Town').value = townRaw
            }

            document.getElementById('County').value = county
            document.getElementById('Postcode').value = postcode
        }
    })
}

$(function () {
    updateMembershipDescription();

    $('#MembershipCategory').on('change', updateMembershipDescription);
    $('#HasCdhId').on('change', function () {
        $('#cdh-section').toggle(this.checked);
    });

    $('form').on('submit', function (e) {
        if (!$(this).valid()) {
            return false;
        }
        $('#submit-button').attr('disabled', true).attr('aria-busy', 'true');
        $('#form-loading').removeClass('sr-only').attr('aria-hidden', 'false');
        return true;
    });
    
    $('#genderNote').toggle($('#Gender').val() === 'Other');
    $('#cdh-section').toggle($('#HasCdhId').is(':checked'));

    // Mutual messaging for form size
    let lastHeight = 0;

    function debounce(func, wait = 100) {
        let timeout;
        return function (...args) {
            clearTimeout(timeout);
            timeout = setTimeout(() => func.apply(this, args), wait);
        };
    }
    function sendHeight() {
        const formContainer = document.querySelector('main') || document.body;
        const newHeight = formContainer.scrollHeight;
        if (Math.abs(newHeight - lastHeight) > 5) {  
            console.log('📏 Sending height:', newHeight);
            window.parent.postMessage({ frameHeight: newHeight }, window.location.origin);
            lastHeight = newHeight;
        }
    }

    // 👇 Wrap the sendHeight
    const debouncedSendHeight = debounce(sendHeight, 100);

    // 👇 MutationObserver using debouncedSendHeight
    const observer = new MutationObserver(debouncedSendHeight);
    observer.observe(document.body, { attributes: true, childList: true, subtree: true });

    // 👇 Resize listener using debouncedSendHeight
    window.addEventListener('resize', debouncedSendHeight);

    // 👇 Trigger once on load to set initial size
    sendHeight();
});

// FingerprintJS init
FingerprintJS.load().then(fp => fp.get()).then(result => {
    const fingerprint = result.visitorId;
    const fingerprintInput = document.getElementById('Fingerprint');
    if (fingerprintInput) {
        fingerprintInput.value = fingerprint;
    }
});


