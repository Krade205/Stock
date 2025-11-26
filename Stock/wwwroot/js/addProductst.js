import { generateQR } from './generateQR.js';

document.addEventListener("DOMContentLoaded", () => {
    const form = document.getElementById("product-form");
    const tableBody = document.querySelector("#product-table tbody");

    form.addEventListener("submit", (e) => {
        e.preventDefault();

        const sku = form.sku.value;
        const name = form.name.value;
        const quantity = form.quantity.value;
        const price = form.price.value;
        const description = form.description.value;
        const imageFile = form.image.files[0];

        const reader = new FileReader();
        reader.onload = function () {
            const imageUrl = reader.result;

            const row = document.createElement("tr");
            row.innerHTML = `
                <td><img src="${imageUrl}" alt="${name}"></td>
                <td>${sku}</td>
                <td>${name}</td>
                <td>${quantity}</td>
                <td>${price}</td>
                <td>${description}</td>
                <td><canvas id="qr-${sku}"></canvas></td>
            `;
            tableBody.appendChild(row);

            generateQR(`qr-${sku}`, sku);
            form.reset();
        };
        reader.readAsDataURL(imageFile);
    });
});