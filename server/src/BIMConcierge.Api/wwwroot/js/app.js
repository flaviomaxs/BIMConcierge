// ── State ────────────────────────────────────────────────────────────────────
let selectedPlan = null;

// ── Load Plans ──────────────────────────────────────────────────────────────
document.addEventListener('DOMContentLoaded', () => {
    loadPlans();
    checkSuccess();
    initNavScroll();
});

async function loadPlans() {
    const container = document.getElementById('plans-container');
    try {
        const response = await fetch('/v1/public/plans');
        const plans = await response.json();
        container.innerHTML = plans.map(renderPlanCard).join('');
    } catch {
        container.innerHTML = '<p class="plans-loading">Erro ao carregar planos. Tente novamente.</p>';
    }
}

function renderPlanCard(plan) {
    const isFeatured = plan.Plan === 'Professional';
    const isFree = plan.Price === 0;
    const originalPriceDisplay = plan.OriginalPrice
        ? `<span class="plan-original-price">De <s>R$ ${formatPrice(plan.OriginalPrice)}</s>/ano</span>`
        : '';
    const priceDisplay = isFree
        ? '<span class="plan-price">Grátis</span>'
        : `${originalPriceDisplay}
           <span class="plan-price">
            <span class="plan-launch-label">Por</span> <span class="currency">R$</span>${formatPrice(plan.Price)}<span class="period">/ano</span>
           </span>`;

    const buttonText = isFree ? 'Começar Grátis' : 'Assinar Agora';
    const buttonAction = isFree
        ? `onclick="openTrialModal()"`
        : `onclick="openCheckout('${plan.Plan}', ${plan.Price})"`;

    const features = plan.Features.map(f => `<li>${f}</li>`).join('');

    return `
        <div class="plan-card ${isFeatured ? 'plan-featured' : ''}">
            <div class="plan-name">${plan.Plan}</div>
            ${priceDisplay}
            <div class="plan-meta">${plan.Seats} seat${plan.Seats > 1 ? 's' : ''} · ${plan.DurationDays} dias</div>
            <ul class="plan-features">${features}</ul>
            <div class="plan-btn">
                <button class="btn ${isFeatured ? 'btn-primary' : 'btn-outline'} btn-lg btn-full" ${buttonAction}>
                    ${buttonText}
                </button>
            </div>
        </div>
    `;
}

function formatPrice(price) {
    return price.toFixed(2).replace('.', ',');
}

// ── Checkout Modal ──────────────────────────────────────────────────────────
function openCheckout(plan, price) {
    selectedPlan = plan;
    document.getElementById('modal-plan-name').textContent =
        `Plano ${plan} — R$ ${formatPrice(price)}/ano`;
    document.getElementById('checkout-email').value = '';
    document.getElementById('checkout-modal').style.display = 'flex';
    document.getElementById('checkout-email').focus();
}

function closeModal() {
    document.getElementById('checkout-modal').style.display = 'none';
    selectedPlan = null;
}

// Close modal on overlay click
document.addEventListener('click', (e) => {
    if (e.target.id === 'checkout-modal') closeModal();
});

// Close modal on Escape
document.addEventListener('keydown', (e) => {
    if (e.key === 'Escape') closeModal();
});

async function handleCheckout(e) {
    e.preventDefault();
    const email = document.getElementById('checkout-email').value.trim();
    const btn = document.getElementById('checkout-btn');

    if (!email || !selectedPlan) return;

    btn.textContent = 'Redirecionando...';
    btn.disabled = true;

    try {
        const response = await fetch('/v1/public/checkout', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ Plan: selectedPlan, Email: email })
        });

        if (!response.ok) {
            const error = await response.json();
            throw new Error(error.error || 'Erro ao criar checkout');
        }

        const data = await response.json();
        if (data.CheckoutUrl) {
            window.location.href = data.CheckoutUrl;
        } else {
            throw new Error('URL de checkout não retornada');
        }
    } catch (err) {
        alert(err.message || 'Erro ao processar. Tente novamente.');
        btn.textContent = 'Ir para pagamento';
        btn.disabled = false;
    }
}

// ── Success Page ────────────────────────────────────────────────────────────
function checkSuccess() {
    const params = new URLSearchParams(window.location.search);
    if (params.has('session_id') || window.location.pathname === '/sucesso') {
        document.getElementById('success-page').style.display = 'flex';
    }
}

// ── FAQ Toggle ──────────────────────────────────────────────────────────────
function toggleFaq(button) {
    const item = button.closest('.faq-item');
    const wasOpen = item.classList.contains('open');

    // Close all
    document.querySelectorAll('.faq-item.open').forEach(el => el.classList.remove('open'));

    // Toggle current
    if (!wasOpen) item.classList.add('open');
}

// ── Trial Modal ────────────────────────────────────────────────────────────
function openTrialModal() {
    document.getElementById('trial-name').value = '';
    document.getElementById('trial-email').value = '';
    document.getElementById('trial-modal').style.display = 'flex';
    document.getElementById('trial-name').focus();
}

function closeTrialModal() {
    document.getElementById('trial-modal').style.display = 'none';
}

// Close trial modal on overlay click
document.addEventListener('click', (e) => {
    if (e.target.id === 'trial-modal') closeTrialModal();
});

async function handleTrial(e) {
    e.preventDefault();
    const email = document.getElementById('trial-email').value.trim();
    const name = document.getElementById('trial-name').value.trim();
    const btn = document.getElementById('trial-btn');

    if (!email) return;

    btn.textContent = 'Ativando...';
    btn.disabled = true;

    try {
        const response = await fetch('/v1/public/trial', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ Email: email, Name: name || null })
        });

        const data = await response.json();

        if (!response.ok) {
            throw new Error(data.error || 'Erro ao ativar Trial');
        }

        // Show success page
        closeTrialModal();
        document.getElementById('success-page').style.display = 'flex';
    } catch (err) {
        alert(err.message || 'Erro ao processar. Tente novamente.');
        btn.textContent = 'Ativar Trial';
        btn.disabled = false;
    }
}

// ── Nav Scroll Effect ───────────────────────────────────────────────────────
function initNavScroll() {
    const nav = document.getElementById('nav');
    window.addEventListener('scroll', () => {
        nav.style.borderBottomColor = window.scrollY > 50 ? 'var(--border)' : 'transparent';
    });
}
