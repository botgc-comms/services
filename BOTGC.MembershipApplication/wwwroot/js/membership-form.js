function updateMembershipDescription() {

    const descriptions = {
        "7Day": "Our Full Membership gives you unrestricted access to the course and all club facilities, seven days a week. You’ll also be eligible to play in all club competitions and enjoy full voting rights as a member.",
        "6Day": "Six Day Membership allows you to play and use the facilities from Sunday to Friday. You can still take part in many competitions and events — just note that play on Saturdays is not included.",
        "5Day": "With a Five Day Membership, you can enjoy access to the course and facilities Monday to Friday. This is ideal for those who prefer to play during quieter weekday periods. Weekend access is not included.",
        "Intermediate": "Our Intermediate (Affordable) Membership is designed to help younger golfers or those new to the game progress toward full membership. It's age-restricted and offers a great value route into the club with increasing access over time.",
        "Flexi": "A flexible membership for occasional golfers who prefer to play at quieter times. Access to the course is limited to off-peak hours and you won’t be eligible for competitions, but it’s a great option for more casual play.",
        "Junior": "Junior Membership is open to younger golfers and includes full access to the course and practice facilities. Juniors are encouraged to get involved with coaching, competitions, and club events to develop their game.",
        "Student": "This membership is for those in full-time education and provides access to the course with some restrictions. It’s a great option for students who want to stay active and play regularly while studying.",
        "Social": "Social Membership gives you access to the clubhouse, social events, and a limited number of golf opportunities. You’ll also benefit from discounted food and drink in the clubhouse and reduced entry fees to club events.",
        "Clubhouse": "Our Clubhouse Membership is for those who want to enjoy the social side of the club — including access to the bar, restaurant, and club events — without using the golf course. Clubhouse members receive discounts on food, drink, and event entry.",
        "Family": "Family Membership provides access to the clubhouse facilities and events for relatives of Full or Junior Members. It’s ideal for those who want to be involved in the club socially and support family members who play. Family members also enjoy discounted food, drink, and entry to club events."
    };

    const selectedValue = $('#MembershipCategory').val();
    const $desc = $('#membership-description');
    let description = descriptions[selectedValue] || '';

    const financeEligible = ['7Day', '6Day', '5Day', 'Intermediate'].includes(selectedValue);
    $('#finance-section').toggle(financeEligible);

    // Check if there's a referrer present on the page (hidden field or model)
    const referralEligibleCategories = ['7Day', '6Day', '5Day', 'Intermediate', 'Flexi'];
    const hasReferrer = $('#ReferrerId').val() || false;

    // Append referral reward message if applicable
    if (hasReferrer) {
        if (referralEligibleCategories.includes(selectedValue)) {
            description += " <strong>Great news – this category qualifies for our referral reward scheme.</strong>";
        } else {
            description += " <strong>Note: This category is not eligible for the referral reward scheme.</strong>";
        }
    }

    $desc.html(description).toggle(!!selectedValue);
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


