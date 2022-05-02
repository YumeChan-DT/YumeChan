function setMainLayout(layoutType: 'default' | 'fluid') {
    const layout = document.getElementById("main-container");
    
    if (layoutType === 'default') {
        layout.setAttribute('class', 'container');
    } else  if (layoutType === 'fluid') {
        layout.setAttribute('class', 'container-fluid');
        
        let mainNavbarHeight = document.getElementById("main-navbar").offsetHeight;
        
        layout.setAttribute('style', `margin-top: ${mainNavbarHeight}px`);
    }
}