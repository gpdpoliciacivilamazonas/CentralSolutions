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
			const url = button.dataset.shareUrl || window.location.href;

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

	document.querySelectorAll('[data-attachment-input]').forEach((input) => {
		const preview = input.parentElement?.querySelector('[data-attachment-preview]');
		if (!preview) {
			return;
		}

		const dataTransfer = new DataTransfer();

		const formatSize = (bytes) => {
			if (bytes < 1024) return `${bytes} B`;
			if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
			return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
		};

		const renderPreview = () => {
			preview.innerHTML = '';
			const files = Array.from(dataTransfer.files);
			preview.hidden = files.length === 0;

			files.forEach((file, index) => {
				const ext = (file.name.split('.').pop() || '').toUpperCase();
				const item = document.createElement('div');
				item.className = 'attachment-item';

				const info = document.createElement('div');
				info.className = 'd-flex align-items-center gap-2';

				const badge = document.createElement('span');
				badge.className = 'attachment-badge';
				badge.textContent = ext;

				const nameWrap = document.createElement('div');
				const nameSpan = document.createElement('span');
				nameSpan.className = 'attachment-name';
				nameSpan.textContent = file.name;

				const sizeSpan = document.createElement('span');
				sizeSpan.className = 'text-muted small ms-2';
				sizeSpan.textContent = `(${formatSize(file.size)})`;

				nameWrap.append(nameSpan, sizeSpan);
				info.append(badge, nameWrap);

				const removeBtn = document.createElement('button');
				removeBtn.type = 'button';
				removeBtn.className = 'btn btn-sm btn-outline-danger';
				removeBtn.textContent = 'Remover';
				removeBtn.addEventListener('click', () => {
					dataTransfer.items.remove(index);
					input.files = dataTransfer.files;
					renderPreview();
				});

				item.append(info, removeBtn);
				preview.appendChild(item);
			});
		};

		input.addEventListener('change', () => {
			Array.from(input.files).forEach((file) => {
				const exists = Array.from(dataTransfer.files).some(
					(existing) => existing.name === file.name && existing.size === file.size && existing.lastModified === file.lastModified
				);
				if (!exists) {
					dataTransfer.items.add(file);
				}
			});
			input.files = dataTransfer.files;
			renderPreview();
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
