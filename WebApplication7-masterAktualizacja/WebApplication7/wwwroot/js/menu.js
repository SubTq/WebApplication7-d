document.addEventListener("DOMContentLoaded", function () {
    const hamburger = document.getElementById("hamburger-menu");
    const menuList = document.getElementById("menu-list");

    // Rozwijanie menu po najechaniu na hamburger
    hamburger.addEventListener("mouseenter", () => {
        menuList.style.display = "block";
    });

    // Zwinięcie menu po opuszczeniu hamburgera
    hamburger.addEventListener("mouseleave", () => {
        setTimeout(() => {
            menuList.style.display = "none";
        }, 300);
    });

    // Utrzymanie rozwiniętego menu przy najechaniu na listę
    menuList.addEventListener("mouseenter", () => {
        menuList.style.display = "block";
    });

    menuList.addEventListener("mouseleave", () => {
        menuList.style.display = "none";
    });
});
