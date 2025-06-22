
window.addEventListener('DOMContentLoaded', function () {
    console.log('🚀 Loading Enhanced Swagger UI...');

    setTimeout(function () {
        enhanceSwaggerUI();
        addCoolFeatures();
        addKeyboardShortcuts();
        initializeDarkMode();
        console.log('✨ Enhanced Swagger UI loaded successfully!');
    }, 1000);
});

function enhanceSwaggerUI() {
    addStatusHeader();

    addExpandCollapseButtons();

    addEndpointCounter();

    addResponseTimeIndicator();

    addLoadingStates();
}

function addStatusHeader() {
    const infoSection = document.querySelector('.swagger-ui .info');
    if (infoSection && !document.querySelector('.api-status-header')) {
        const header = document.createElement('div');
        header.className = 'api-status-header';
        header.innerHTML = `
            <div style="text-align: center; margin-bottom: 2rem;">
                <div style="display: inline-block; padding: 10px 20px; background: linear-gradient(45deg, #4CAF50, #45a049); border-radius: 25px; color: white; font-weight: 600; margin-bottom: 1rem; box-shadow: 0 4px 8px rgba(76, 175, 80, 0.3);">
                    🚀 API Status: <span style="color: #90EE90; font-weight: 700;">ONLINE</span>
                </div>
                <div style="font-size: 0.9rem; color: #666; margin-top: 0.5rem; font-weight: 500;">
                    ⚡ Powered by .NET 9 | 🗄️ PostgreSQL | 🚀 Redis Cache | 📊 Real-time Analytics
                </div>
                <div style="font-size: 0.8rem; color: #999; margin-top: 0.5rem;">
                    Last updated: ${new Date().toLocaleString()}
                </div>
            </div>
        `;
        infoSection.insertBefore(header, infoSection.firstChild);
    }
}

function addExpandCollapseButtons() {
    const wrapper = document.querySelector('.swagger-ui .wrapper');
    if (wrapper && !document.querySelector('.control-buttons')) {
        const buttonContainer = document.createElement('div');
        buttonContainer.className = 'control-buttons';
        buttonContainer.style.cssText = `
            text-align: center; 
            margin: 20px 0; 
            display: flex; 
            gap: 10px; 
            justify-content: center;
            flex-wrap: wrap;
            padding: 15px;
            background: rgba(255, 255, 255, 0.8);
            border-radius: 12px;
            backdrop-filter: blur(10px);
            box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
        `;

        buttonContainer.innerHTML = `
            <button id="expandAll" class="swagger-ui-btn" title="Expand all endpoints (Ctrl+E)">
                📖 Expand All
            </button>
            <button id="collapseAll" class="swagger-ui-btn" title="Collapse all endpoints (Ctrl+C)">
                📕 Collapse All
            </button>
            <button id="toggleDark" class="swagger-ui-btn" title="Toggle dark mode (Ctrl+D)">
                🌙 Dark Mode
            </button>
            <button id="copyUrl" class="swagger-ui-btn" title="Copy current URL">
                🔗 Copy URL
            </button>
            <button id="refreshApi" class="swagger-ui-btn" title="Refresh API documentation">
                🔄 Refresh
            </button>
        `;

        const infoSection = document.querySelector('.swagger-ui .info');
        if (infoSection) {
            infoSection.parentNode.insertBefore(buttonContainer, infoSection.nextSibling);
        }

        document.getElementById('expandAll').addEventListener('click', expandAllOperations);
        document.getElementById('collapseAll').addEventListener('click', collapseAllOperations);
        document.getElementById('toggleDark').addEventListener('click', toggleDarkMode);
        document.getElementById('copyUrl').addEventListener('click', copyCurrentUrl);
        document.getElementById('refreshApi').addEventListener('click', refreshApi);
    }
}

function expandAllOperations() {
    const collapsedOperations = document.querySelectorAll('.swagger-ui .opblock:not(.is-open)');
    let count = 0;

    collapsedOperations.forEach((op, index) => {
        setTimeout(() => {
            const summary = op.querySelector('.opblock-summary');
            if (summary) {
                summary.click();
                count++;
            }
        }, index * 50);
    });

    showNotification(`📖 Expanding ${collapsedOperations.length} operations...`, 'success');
}

function collapseAllOperations() {
    const openOperations = document.querySelectorAll('.swagger-ui .opblock.is-open');
    let count = 0;

    openOperations.forEach((op, index) => {
        setTimeout(() => {
            const summary = op.querySelector('.opblock-summary');
            if (summary) {
                summary.click();
                count++;
            }
        }, index * 50);
    });

    showNotification(`📕 Collapsing ${openOperations.length} operations...`, 'success');
}

function toggleDarkMode() {
    document.body.classList.toggle('dark-mode');
    const isDark = document.body.classList.contains('dark-mode');
    localStorage.setItem('swagger-dark-mode', isDark);

    const button = document.getElementById('toggleDark');
    if (button) {
        button.innerHTML = isDark ? '☀️ Light Mode' : '🌙 Dark Mode';
    }

    showNotification(isDark ? '🌙 Dark mode enabled' : '☀️ Light mode enabled', 'info');
}

function copyCurrentUrl() {
    navigator.clipboard.writeText(window.location.href).then(() => {
        showNotification('🔗 URL copied to clipboard!', 'success');
    }).catch(() => {
        showNotification('❌ Failed to copy URL', 'error');
    });
}

function refreshApi() {
    showNotification('🔄 Refreshing API documentation...', 'info');
    setTimeout(() => {
        window.location.reload();
    }, 1000);
}

function addEndpointCounter() {
    if (document.querySelector('.endpoint-counter')) return;

    const operations = document.querySelectorAll('.swagger-ui .opblock');
    const methodCounts = {};

    operations.forEach(op => {
        const method = op.className.match(/opblock-(\w+)/)?.[1];
        if (method) {
            methodCounts[method] = (methodCounts[method] || 0) + 1;
        }
    });

    const counter = document.createElement('div');
    counter.className = 'endpoint-counter';
    counter.style.cssText = `
        position: fixed;
        top: 20px;
        right: 20px;
        background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
        color: white;
        padding: 12px 20px;
        border-radius: 25px;
        font-weight: 600;
        font-size: 0.9rem;
        box-shadow: 0 4px 12px rgba(102, 126, 234, 0.3);
        z-index: 1000;
        cursor: pointer;
        transition: all 0.3s ease;
        backdrop-filter: blur(10px);
    `;

    const methodEmojis = {
        get: '📖',
        post: '📝',
        put: '✏️',
        delete: '🗑️'
    };

    const methodStats = Object.entries(methodCounts)
        .map(([method, count]) => `${methodEmojis[method] || '🔧'} ${count}`)
        .join(' ');

    counter.innerHTML = `
        <div style="font-size: 1rem; margin-bottom: 4px;">📊 ${operations.length} Endpoints</div>
        <div style="font-size: 0.75rem; opacity: 0.9;">${methodStats}</div>
    `;

    counter.addEventListener('mouseenter', () => {
        counter.style.transform = 'scale(1.05)';
        counter.style.boxShadow = '0 6px 20px rgba(102, 126, 234, 0.4)';
    });

    counter.addEventListener('mouseleave', () => {
        counter.style.transform = 'scale(1)';
        counter.style.boxShadow = '0 4px 12px rgba(102, 126, 234, 0.3)';
    });

    document.body.appendChild(counter);
}

function addResponseTimeIndicator() {
    const originalFetch = window.fetch;
    window.fetch = function (...args) {
        const startTime = performance.now();
        return originalFetch.apply(this, args).then(response => {
            const endTime = performance.now();
            const responseTime = Math.round(endTime - startTime);
            showResponseTime(responseTime);
            return response;
        }).catch(error => {
            const endTime = performance.now();
            const responseTime = Math.round(endTime - startTime);
            showResponseTime(responseTime, true);
            throw error;
        });
    };
}

function showResponseTime(time, isError = false) {
    const existing = document.querySelector('.response-time-indicator');
    if (existing) existing.remove();

    const indicator = document.createElement('div');
    indicator.className = 'response-time-indicator';
    indicator.style.cssText = `
        position: fixed;
        top: 100px;
        right: 20px;
        background: ${isError ? '#F44336' : time < 500 ? '#4CAF50' : time < 1000 ? '#FF9800' : '#F44336'};
        color: white;
        padding: 8px 16px;
        border-radius: 20px;
        font-size: 0.8rem;
        font-weight: 600;
        z-index: 1000;
        animation: slideInRight 0.3s ease;
        box-shadow: 0 4px 12px rgba(0, 0, 0, 0.2);
        backdrop-filter: blur(10px);
    `;

    const emoji = isError ? '❌' : time < 500 ? '⚡' : time < 1000 ? '⏱️' : '🐌';
    indicator.innerHTML = `${emoji} ${isError ? 'Error' : time + 'ms'}`;

    document.body.appendChild(indicator);

    setTimeout(() => {
        if (indicator.parentNode) {
            indicator.style.animation = 'slideOutRight 0.3s ease';
            setTimeout(() => indicator.remove(), 300);
        }
    }, 3000);
}

function addLoadingStates() {
    document.addEventListener('click', function (e) {
        if (e.target.classList.contains('btn') && e.target.textContent.includes('Execute')) {
            const originalText = e.target.textContent;
            const originalBg = e.target.style.background;

            e.target.style.background = 'linear-gradient(135deg, #ffa726 0%, #ff7043 100%)';
            e.target.innerHTML = '⏳ Executing...';
            e.target.disabled = true;

            const observer = new MutationObserver((mutations) => {
                mutations.forEach((mutation) => {
                    if (mutation.type === 'childList') {
                        const responseSection = e.target.closest('.opblock').querySelector('.responses-wrapper .response');
                        if (responseSection) {
                            setTimeout(() => {
                                e.target.style.background = originalBg;
                                e.target.innerHTML = originalText;
                                e.target.disabled = false;
                                observer.disconnect();
                            }, 500);
                        }
                    }
                });
            });

            observer.observe(e.target.closest('.opblock'), { childList: true, subtree: true });
        }
    });
}

function addKeyboardShortcuts() {
    document.addEventListener('keydown', function (e) {
        if (e.target.tagName === 'INPUT' || e.target.tagName === 'TEXTAREA') return;

        if ((e.ctrlKey || e.metaKey) && e.key === 'e') {
            e.preventDefault();
            expandAllOperations();
        }

        if ((e.ctrlKey || e.metaKey) && e.key === 'c' && !e.target.value) {
            e.preventDefault();
            collapseAllOperations();
        }

        if ((e.ctrlKey || e.metaKey) && e.key === 'd') {
            e.preventDefault();
            toggleDarkMode();
        }

        if (e.key === 'Escape') {
            collapseAllOperations();
        }

        if (e.key === 'F5' || ((e.ctrlKey || e.metaKey) && e.key === 'r')) {
            showNotification('🔄 Refreshing...', 'info');
        }
    });

    addKeyboardShortcutsHelp();
}

function addKeyboardShortcutsHelp() {
    if (document.querySelector('.keyboard-help')) return;

    const help = document.createElement('div');
    help.className = 'keyboard-help';
    help.style.cssText = `
        position: fixed;
        bottom: 20px;
        left: 20px;
        background: rgba(0, 0, 0, 0.85);
        color: white;
        padding: 15px 20px;
        border-radius: 12px;
        font-size: 0.8rem;
        z-index: 1000;
        max-width: 250px;
        opacity: 0.8;
        transition: all 0.3s ease;
        backdrop-filter: blur(10px);
        border: 1px solid rgba(255, 255, 255, 0.1);
    `;
    help.innerHTML = `
        <div style="font-weight: 600; margin-bottom: 8px; font-size: 0.9rem;">⌨️ Keyboard Shortcuts:</div>
        <div style="margin-bottom: 4px;"><kbd>Ctrl+E</kbd> - Expand All</div>
        <div style="margin-bottom: 4px;"><kbd>Ctrl+C</kbd> - Collapse All</div>
        <div style="margin-bottom: 4px;"><kbd>Ctrl+D</kbd> - Dark Mode</div>
        <div style="margin-bottom: 4px;"><kbd>Esc</kbd> - Close All</div>
        <div style="font-size: 0.7rem; opacity: 0.7; margin-top: 8px;">Hover to see more</div>
    `;

    help.addEventListener('mouseenter', () => {
        help.style.opacity = '1';
        help.style.transform = 'scale(1.05)';
    });
    help.addEventListener('mouseleave', () => {
        help.style.opacity = '0.8';
        help.style.transform = 'scale(1)';
    });

    document.body.appendChild(help);
}

function showNotification(message, type = 'info') {
    const notification = document.createElement('div');
    notification.className = 'notification';
    notification.style.cssText = `
        position: fixed;
        top: 20px;
        left: 50%;
        transform: translateX(-50%);
        background: ${type === 'success' ? '#4CAF50' : type === 'error' ? '#F44336' : '#2196F3'};
        color: white;
        padding: 12px 24px;
        border-radius: 25px;
        font-weight: 600;
        z-index: 1001;
        animation: slideDown 0.3s ease;
        box-shadow: 0 4px 20px rgba(0, 0, 0, 0.2);
        backdrop-filter: blur(10px);
        font-size: 0.9rem;
    `;
    notification.textContent = message;

    if (!document.querySelector('#notification-styles')) {
        const style = document.createElement('style');
        style.id = 'notification-styles';
        style.textContent = `
            @keyframes slideDown {
                from { transform: translateX(-50%) translateY(-100%); opacity: 0; }
                to { transform: translateX(-50%) translateY(0); opacity: 1; }
            }
            @keyframes slideInRight {
                from { transform: translateX(100%); opacity: 0; }
                to { transform: translateX(0); opacity: 1; }
            }
            @keyframes slideOutRight {
                from { transform: translateX(0); opacity: 1; }
                to { transform: translateX(100%); opacity: 0; }
            }
            kbd {
                background: rgba(255, 255, 255, 0.2);
                border: 1px solid rgba(255, 255, 255, 0.3);
                border-radius: 4px;
                padding: 2px 6px;
                font-size: 0.7rem;
                font-family: monospace;
            }
        `;
        document.head.appendChild(style);
    }

    document.body.appendChild(notification);

    setTimeout(() => {
        if (notification.parentNode) {
            notification.style.animation = 'slideDown 0.3s ease reverse';
            setTimeout(() => notification.remove(), 300);
        }
    }, 2500);
}

function addCoolFeatures() {
    document.documentElement.style.scrollBehavior = 'smooth';

    const operations = document.querySelectorAll('.swagger-ui .opblock');
    operations.forEach(op => {
        op.addEventListener('mouseenter', function () {
            this.style.transform = 'translateY(-2px) scale(1.01)';
            this.style.transition = 'all 0.3s ease';
        });

        op.addEventListener('mouseleave', function () {
            this.style.transform = 'translateY(0) scale(1)';
        });
    });

    document.addEventListener('click', function (e) {
        if (e.target.classList.contains('swagger-ui-btn')) {
            e.target.style.transform = 'scale(0.95)';
            setTimeout(() => {
                e.target.style.transform = '';
            }, 150);
        }
    });

    addProgressIndicator();
}

function addProgressIndicator() {
    const progress = document.createElement('div');
    progress.style.cssText = `
        position: fixed;
        top: 0;
        left: 0;
        width: 0%;
        height: 3px;
        background: linear-gradient(90deg, #667eea, #764ba2);
        z-index: 9999;
        transition: width 0.3s ease;
    `;
    document.body.appendChild(progress);

    let width = 0;
    const interval = setInterval(() => {
        width += Math.random() * 10;
        if (width >= 100) {
            width = 100;
            clearInterval(interval);
            setTimeout(() => progress.remove(), 500);
        }
        progress.style.width = width + '%';
    }, 100);
}

function initializeDarkMode() {
    if (localStorage.getItem('swagger-dark-mode') === 'true') {
        document.body.classList.add('dark-mode');
        const button = document.getElementById('toggleDark');
        if (button) {
            button.innerHTML = '☀️ Light Mode';
        }
    }
}

function addEasterEggs() {
    let clickCount = 0;
    const title = document.querySelector('.swagger-ui .info .title');

    if (title) {
        title.addEventListener('click', () => {
            clickCount++;
            if (clickCount === 5) {
                showNotification('🎉 You found the easter egg! You\'re awesome! 🚀', 'success');
                createConfetti();
                clickCount = 0;
            }
        });
    }
}

function createConfetti() {
    const colors = ['#667eea', '#764ba2', '#4CAF50', '#FF9800', '#F44336'];
    for (let i = 0; i < 50; i++) {
        setTimeout(() => {
            const confetti = document.createElement('div');
            confetti.style.cssText = `
                position: fixed;
                top: -10px;
                left: ${Math.random() * 100}%;
                width: 10px;
                height: 10px;
                background: ${colors[Math.floor(Math.random() * colors.length)]};
                animation: confetti-fall 3s linear forwards;
                z-index: 9999;
            `;

            if (!document.querySelector('#confetti-styles')) {
                const style = document.createElement('style');
                style.id = 'confetti-styles';
                style.textContent = `
                    @keyframes confetti-fall {
                        to {
                            transform: translateY(100vh) rotate(360deg);
                            opacity: 0;
                        }
                    }
                `;
                document.head.appendChild(style);
            }

            document.body.appendChild(confetti);
            setTimeout(() => confetti.remove(), 3000);
        }, i * 50);
    }
}

setTimeout(() => {
    addEasterEggs();
}, 2000);

console.log('🎨 Custom Swagger UI JavaScript loaded successfully!');