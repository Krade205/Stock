const container = document.querySelector('.container');
const registerBtn = document.querySelector('.register-btn');
const loginBtn = document.querySelector('.login-btn');

registerBtn.addEventListener('click', () => {
    container.classList.add('active');
})

loginBtn.addEventListener('click', () => {
    container.classList.remove('active');
})

window.onload = function () {
    const showRegister = document.getElementById('showRegister')?.value;
    if (showRegister === "True" || showRegister === "true") {
        document.querySelector('.container').classList.add('active');
    }
};