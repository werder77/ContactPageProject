// This file handles the form submission logic
const contactForm = document.getElementById('contactForm');
const responseText = document.getElementById('response');

if (contactForm) {
    contactForm.addEventListener('submit', async (e) => {
        e.preventDefault();

        // Automatically grab all fields with a "name" attribute
        const fd = new FormData(contactForm);
        const formData = Object.fromEntries(fd.entries());

        try {
            responseText.innerText = "Sending...";
            
            const response = await fetch("/api/SendContactEmail", {
                method: "POST",
                headers: {
                    "Content-Type": "application/json"
                },
                body: JSON.stringify(formData)
            });
            
            const result = await response.text();
            
            if (response.ok) {
                responseText.style.color = "green";
                contactForm.reset(); 
            } else {
                responseText.style.color = "red";
            }
            
            responseText.innerText = result;
        } catch (error) {
            console.error("Error:", error);
            responseText.innerText = "Failed to connect to the server.";
        }
    });
}