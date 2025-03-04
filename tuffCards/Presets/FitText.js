window.addEventListener('load', () => resizeAll())

function resizeAll() {
    const fitList = document.getElementsByClassName("fit");
    for (fit of fitList) {
        resize(fit);
    }
}

function resize(fit) {
    const parentStyle = window.getComputedStyle(fit.parentNode);
    const parentWidth = fit.parentNode.clientWidth - parseInt(parentStyle.paddingLeft) - parseInt(parentStyle.paddingRight);
    const parentHeight = fit.parentNode.clientHeight - parseInt(parentStyle.paddingTop) - parseInt(parentStyle.paddingBottom);
    const initialSize = window.getComputedStyle(fit).fontSize;
    var size = parseInt(initialSize);
    while (fit.scrollWidth > parentWidth || fit.scrollHeight > parentHeight) {
        size--;
        console.log(size);
        fit.style.setProperty('font-size', size + 'px');
    }
}