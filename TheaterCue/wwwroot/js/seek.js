window.seekHelpers = {
    setSliderValue: function (elementId, value) {
        const el = document.getElementById(elementId);
        if (el) el.value = value;
    }
};