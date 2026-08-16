// adelux horizontal slider

const slides = document.querySelectorAll(".adelux-slider .slide");
const nextBtn = document.querySelector(".next");
const prevBtn = document.querySelector(".prev");

let index = 0;

function showSlide(i) {
    slides.forEach((slide, idx) => {
        slide.classList.remove("active", "prev-active");

        if (idx === i) {
            slide.classList.add("active");
        }
        if (idx === (i - 1 + slides.length) % slides.length) {
            slide.classList.add("prev-active");
        }
    });
}

nextBtn.addEventListener("click", () => {
    index = (index + 1) % slides.length;
    showSlide(index);
});

prevBtn.addEventListener("click", () => {
    index = (index - 1 + slides.length) % slides.length;
    showSlide(index);
});

// auto slide
setInterval(() => {
    index = (index + 1) % slides.length;
    showSlide(index);
}, 4500);