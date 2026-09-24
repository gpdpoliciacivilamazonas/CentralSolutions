// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

document.addEventListener('DOMContentLoaded', () => {
	const themeToggle = document.querySelector('[data-theme-toggle]');
	const themeIcon = document.querySelector('[data-theme-icon]');

	const updateThemeButton = () => {
		const isDark = document.documentElement.dataset.theme === 'dark';

		if (themeIcon) {
			themeIcon.textContent = isDark ? '☀️' : '🌙';
		}
	};

	if (themeToggle) {
		updateThemeButton();

		themeToggle.addEventListener('click', () => {
			const nextTheme = document.documentElement.dataset.theme === 'dark' ? 'light' : 'dark';
			document.documentElement.dataset.theme = nextTheme;
			localStorage.setItem('theme', nextTheme);
			updateThemeButton();
		});
	}

	document.querySelectorAll('[data-share-ticket]').forEach((button) => {
		button.addEventListener('click', async () => {
			const url = window.location.href;

			if (navigator.share) {
				await navigator.share({
					title: document.title,
					url: url
				});

				return;
			}

			await navigator.clipboard.writeText(url);

			const toast = document.querySelector('[data-share-toast]')

			if (toast) {
				bootstrap.Toast.getOrCreateInstance(toast).show();
			}
		});
	});

	document.querySelectorAll('[data-share-ticket-index]').forEach((button) => {
		button.addEventListener('click', async () => {
			const url = button.dataset.shareTicketIndex;

			if (navigator.share) {
				await navigator.share({
					title: 'Chamado',
					url: url
				});

				return;
			}

			await navigator.clipboard.writeText(url);

			const toast = document.querySelector('[data-share-toast]');

			if (toast) {
				bootstrap.Toast.getOrCreateInstance(toast).show();
			}
		});
	});

	document.querySelectorAll('[data-department-combobox]').forEach((combobox) => {
		const input = combobox.querySelector('[data-combobox-input]');
		const toggle = combobox.querySelector('[data-combobox-toggle]');
		const menu = combobox.querySelector('[data-combobox-menu]');
		const options = Array.from(combobox.querySelectorAll('[data-combobox-option]'));

		if (!input || !toggle || !menu) {
			return;
		}

		const close = () => combobox.classList.remove('is-open');
		const open = () => combobox.classList.add('is-open');
		const normalize = (value) => value.toLocaleLowerCase('pt-BR');

		const filterOptions = () => {
			const search = normalize(input.value.trim());

			options.forEach((option) => {
				const matches = normalize(option.dataset.value ?? option.textContent ?? '').includes(search);
				option.hidden = !matches;
			});
		};

		input.addEventListener('input', () => {
			filterOptions();
			open();
		});

		input.addEventListener('focus', () => {
			filterOptions();
		});

		toggle.addEventListener('click', () => {
			if (combobox.classList.contains('is-open')) {
				close();
				return;
			}

			filterOptions();
			open();
			input.focus();
		});

		options.forEach((option) => {
			option.addEventListener('click', () => {
				input.value = option.dataset.value ?? option.textContent?.trim() ?? '';
				input.dispatchEvent(new Event('change', { bubbles: true }));
				close();
			});
		});

		document.addEventListener('click', (event) => {
			if (!combobox.contains(event.target)) {
				close();
			}
		});
	});
});
function printTicket() {
  const printDate = document.getElementById("print-date")

  if (printDate) {
    printDate.textContent = new Date().toLocaleDateString("pt-BR")
  }

  window.print()
}
