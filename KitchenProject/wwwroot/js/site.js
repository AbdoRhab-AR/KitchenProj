/**
 * نظام بن قسومة لحلول المطابخ
 * 1. القوائم المنسدلة القابلة للبحث بالاسم عند الكتابة (Searchable Name Combobox) في جميع الشاشات
 * 2. نظام الحماية والتحقق الصارم للبحث بالاسم فقط ومنع تنفيذ أي أوامر أو ثغرات (SQLi / XSS / Command Injection)
 */
(function () {
    'use strict';

    // الكلمات المحجوزة والأوامر البرمجية المحظورة أمنياً
    const BLOCKED_COMMANDS = new Set([
        'select', 'insert', 'update', 'delete', 'drop', 'alter', 'create', 'truncate',
        'exec', 'execute', 'union', 'declare', 'cast', 'convert', 'waitfor', 'delay',
        'benchmark', 'sleep', 'sysobjects', 'syscolumns', 'information_schema',
        'xp_cmdshell', 'sp_executesql', 'table', 'database', 'schema', 'grant', 'revoke',
        'having', 'group', 'order', 'where', 'from', 'into', 'values', 'join', 'inner', 'outer',
        'script', 'javascript', 'vbscript', 'expression', 'onload', 'onerror', 'onclick',
        'onmouseover', 'onfocus', 'onblur', 'onsubmit', 'onchange', 'oninput', 'onkeydown',
        'onkeyup', 'eval', 'alert', 'prompt', 'confirm', 'document', 'window', 'cookie',
        'localstorage', 'sessionstorage', 'location', 'history', 'navigator', 'fetch',
        'xmlhttprequest', 'iframe', 'object', 'embed', 'svg', 'style', 'meta', 'base',
        'cmd', 'powershell', 'pwsh', 'bash', 'sh', 'zsh', 'curl', 'wget', 'netcat', 'nmap',
        'whoami', 'chmod', 'chown', 'sudo', 'erase', 'format', 'shutdown', 'reboot',
        'taskkill', 'ping', 'tracert', 'nslookup', 'system', 'invoke'
    ]);

    // البحث بالاسم فقط: حروف عربية وإنجليزية ومسافات فقط
    const NAME_ONLY_REGEX = /^[\u0621-\u064A\u066E-\u06D3\u064B-\u065Fa-zA-Z\s]+$/;
    const DANGEROUS_SYMBOLS_REGEX = /[<>'"`;=\\|&${}\[\]^~%*+!?@#]|--|\/\*|\*\/|0x[0-9a-fA-F]+/;

    const SECURITY_ERROR_MESSAGE =
        'يُسمح بالبحث بالاسم فقط (حروف عربية أو إنجليزية) ولا يُسمح بإدخال الأرقام أو الرموز أو الأوامر.';

    const KitchenNameValidator = {
        /**
         * يتحقق من أن المدخل اسم صالح فقط وخالٍ من أي أوامر أو رموز أو أرقام
         */
        validateSearch(input) {
            if (!input || !input.trim()) {
                return { valid: true, clean: '', error: null };
            }

            const trimmed = input.trim().replace(/\s+/g, ' ');
            if (trimmed.length > 60) {
                return { valid: false, clean: '', error: SECURITY_ERROR_MESSAGE };
            }

            if (DANGEROUS_SYMBOLS_REGEX.test(trimmed) || !NAME_ONLY_REGEX.test(trimmed)) {
                return { valid: false, clean: '', error: SECURITY_ERROR_MESSAGE };
            }

            const tokens = trimmed.split(' ').filter(Boolean);
            for (const token of tokens) {
                if (BLOCKED_COMMANDS.has(token.toLowerCase())) {
                    return { valid: false, clean: '', error: SECURITY_ERROR_MESSAGE };
                }
            }

            return { valid: true, clean: trimmed, error: null };
        },

        /**
         * يتحقق من حقول الأسماء عند الإدخال لمنع حقن الأكواد أو الأوامر
         */
        validateEntityName(input, strictlyLettersOnly = false) {
            if (!input || !input.trim()) {
                return { valid: true, error: null };
            }
            const trimmed = input.trim();
            if (DANGEROUS_SYMBOLS_REGEX.test(trimmed)) {
                return { valid: false, error: 'الاسم يحتوي على رموز غير مسموح بها أمنياً.' };
            }
            if (strictlyLettersOnly && !NAME_ONLY_REGEX.test(trimmed)) {
                return { valid: false, error: 'يجب أن يحتوي الاسم على حروف عربية أو إنجليزية فقط بدون أرقام أو رموز.' };
            }
            const tokens = trimmed.split(/[\s\-./()]+/).filter(Boolean);
            for (const token of tokens) {
                if (BLOCKED_COMMANDS.has(token.toLowerCase())) {
                    return { valid: false, error: 'يُمنع كتابة أوامر برمجية في حقل الاسم.' };
                }
            }
            return { valid: true, error: null };
        },

        /**
         * توحيد الحروف العربية والإنجليزية لمطابقة ذكية وسريعة بالاسم
         */
        normalize(str) {
            if (!str) return '';
            return str
                .toLowerCase()
                .replace(/[\u064B-\u065F\u0640]/g, '') // إزالة التشكيل والكشيدة
                .replace(/[أإآ]/g, 'ا')
                .replace(/ة/g, 'ه')
                .replace(/ى/g, 'ي')
                .replace(/\s+/g, ' ')
                .trim();
        },

        /**
         * فصل الاسم الصافي عن الأكواد والأرقام (مثل CUST-0001 أو PRJ-0001 أو RM-0001)
         * ليكون البحث مقتصراً على الاسم فقط
         */
        parseOptionText(rawText) {
            const text = (rawText || '').trim();
            if (!text) return { namePart: '', codePart: '', fullText: '' };

            let codePart = '';
            let namePart = text;

            // نمط: "الاسم (CUST-0001)"
            const trailingCodeMatch = text.match(/^(.*?)\s*\(([A-Za-z]+-\d+)\)\s*$/);
            if (trailingCodeMatch) {
                namePart = trailingCodeMatch[1].trim();
                codePart = trailingCodeMatch[2].trim();
            } else {
                // نمط: "PRJ-0001 - اسم المشروع (اسم العميل)" أو "RM-0001 - اسم الخامة"
                const leadingCodeMatch = text.match(/^([A-Za-z0-9]+(?:-\d+)?)\s*[-—]\s*(.+)$/);
                if (leadingCodeMatch) {
                    codePart = leadingCodeMatch[1].trim();
                    namePart = leadingCodeMatch[2].trim();
                }
            }

            // استخلاص الحروف العربية والإنجليزية فقط من جزء الاسم للبحث الصارم بالاسم
            const pureLettersName = namePart
                .replace(/[^a-zA-Z\u0621-\u064A\u066E-\u06D3\u064B-\u065F\s]/g, ' ')
                .replace(/\s+/g, ' ')
                .trim();

            return {
                namePart: namePart,
                pureName: pureLettersName || namePart,
                codePart: codePart,
                fullText: text
            };
        }
    };

    window.KitchenNameValidator = KitchenNameValidator;

    // تحديد القوائم المنسدلة التي تحتوي على أسماء (العملاء، المشاريع، الموردين، الموظفين، الأصناف، المخازن، البنود)
    const SEARCHABLE_SELECT_NAMES = new Set([
        'CustomerId', 'customerId',
        'ProjectId', 'projectId',
        'SupplierId', 'supplierId',
        'SalesUserId', 'EngineerUserId', 'DesignerUserId', 'EngineerId',
        'WarehouseId', 'warehouseId', 'FromWarehouseId', 'ToWarehouseId',
        'ItemId', 'itemId',
        'ItemGroupId', 'groupId'
    ]);

    function shouldEnhanceSelect(select) {
        if (!select || select.dataset.ksEnhanced === 'true') return false;
        if (select.multiple || select.size > 1) return false;
        if (select.dataset.searchable === 'false') return false;
        if (select.dataset.searchable === 'true') return true;
        if (select.classList.contains('item-select') || select.classList.contains('catalog-select') || select.classList.contains('searchable-select')) {
            return true;
        }
        const nameAttr = select.getAttribute('name') || '';
        const fieldAttr = select.dataset.field || '';
        const idAttr = select.getAttribute('id') || '';
        if (SEARCHABLE_SELECT_NAMES.has(nameAttr) || SEARCHABLE_SELECT_NAMES.has(fieldAttr) || SEARCHABLE_SELECT_NAMES.has(idAttr)) {
            return true;
        }
        if (/^(Items|Lines)\[\d+\]\.ItemId$/i.test(nameAttr)) {
            return true;
        }
        return false;
    }

    function escapeHtml(str) {
        return String(str)
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;');
    }

    function highlightMatch(text, query) {
        if (!query) return escapeHtml(text);
        const cleanQuery = query.trim();
        if (!cleanQuery) return escapeHtml(text);

        // بناء تعبير نمطي يراعي تشابه الحروف العربية (أ/إ/آ/ا، ة/ه، ى/ي)
        const pattern = cleanQuery
            .split('')
            .map(ch => {
                if (/[أإآا]/.test(ch)) return '[أإآا][\\u064B-\\u065F]*';
                if (/[ةه]/.test(ch)) return '[ةه][\\u064B-\\u065F]*';
                if (/[ىي]/.test(ch)) return '[ىي][\\u064B-\\u065F]*';
                if (/\s/.test(ch)) return '\\s+';
                return ch.replace(/[.*+?^${}()|[\]\\]/g, '\\$&') + '[\\u064B-\\u065F]*';
            })
            .join('');

        try {
            const regex = new RegExp(`(${pattern})`, 'gi');
            const parts = String(text).split(regex);
            return parts
                .map((part, idx) => (idx % 2 === 1 ? `<mark class="ks-highlight">${escapeHtml(part)}</mark>` : escapeHtml(part)))
                .join('');
        } catch {
            return escapeHtml(text);
        }
    }

    let activeInstance = null;

    class SearchableSelect {
        constructor(select) {
            this.select = select;
            this.select._ksSearchable = this;
            this.select.dataset.ksEnhanced = 'true';
            this.select.classList.add('ks-native-select');
            this.select.tabIndex = -1;

            this.isOpen = false;
            this.activeIndex = -1;
            this.filteredOptions = [];

            this.buildUI();
            this.bindEvents();
            this.syncFromSelect();
        }

        getPlaceholderText() {
            if (this.select.dataset.searchPlaceholder) {
                return this.select.dataset.searchPlaceholder;
            }
            const emptyOpt = Array.from(this.select.options).find(o => o.value === '');
            if (emptyOpt && emptyOpt.textContent.trim()) {
                const raw = emptyOpt.textContent.trim().replace(/^--+\s*|\s*--+$/g, '');
                return `${raw} (اكتب الاسم للبحث...)`;
            }
            return 'اكتب الاسم للبحث أو اختر من القائمة...';
        }

        buildUI() {
            const isSmall = this.select.classList.contains('form-select-sm');
            this.wrapper = document.createElement('div');
            this.wrapper.className = 'ks-combobox' + (isSmall ? ' ks-combobox-sm' : '');

            this.input = document.createElement('input');
            this.input.type = 'text';
            this.input.className = 'form-control ks-combobox-input' + (isSmall ? ' form-control-sm' : '');
            this.input.autocomplete = 'off';
            this.input.spellcheck = false;
            this.input.placeholder = this.getPlaceholderText();

            this.actions = document.createElement('div');
            this.actions.className = 'ks-combobox-actions';

            this.clearBtn = document.createElement('button');
            this.clearBtn.type = 'button';
            this.clearBtn.className = 'ks-clear-btn d-none';
            this.clearBtn.title = 'مسح الاختيار';
            this.clearBtn.innerHTML = '<i class="fa-solid fa-xmark"></i>';

            this.chevron = document.createElement('span');
            this.chevron.className = 'ks-chevron-icon';
            this.chevron.innerHTML = '<i class="fa-solid fa-chevron-down"></i>';

            this.actions.appendChild(this.clearBtn);
            this.actions.appendChild(this.chevron);

            this.wrapper.appendChild(this.input);
            this.wrapper.appendChild(this.actions);

            this.select.parentNode.insertBefore(this.wrapper, this.select.nextSibling);

            this.portal = document.createElement('div');
            this.portal.className = 'ks-dropdown-portal d-none';
            document.body.appendChild(this.portal);
        }

        bindEvents() {
            this.input.addEventListener('focus', () => {
                this.open(true);
            });

            this.input.addEventListener('click', () => {
                if (!this.isOpen) {
                    this.open(true);
                }
            });

            this.input.addEventListener('input', () => {
                if (!this.isOpen) {
                    this.open(false);
                }
                this.renderDropdown(this.input.value);
            });

            this.input.addEventListener('keydown', e => {
                if (e.key === 'ArrowDown') {
                    e.preventDefault();
                    if (!this.isOpen) {
                        this.open(false);
                    } else {
                        this.moveHighlight(1);
                    }
                } else if (e.key === 'ArrowUp') {
                    e.preventDefault();
                    if (this.isOpen) {
                        this.moveHighlight(-1);
                    }
                } else if (e.key === 'Enter') {
                    if (this.isOpen) {
                        e.preventDefault();
                        if (this.activeIndex >= 0 && this.filteredOptions[this.activeIndex]) {
                            this.selectOption(this.filteredOptions[this.activeIndex].option);
                        }
                    }
                } else if (e.key === 'Escape') {
                    if (this.isOpen) {
                        e.preventDefault();
                        this.close();
                    }
                } else if (e.key === 'Tab') {
                    this.close();
                }
            });

            this.clearBtn.addEventListener('mousedown', e => {
                e.preventDefault();
                e.stopPropagation();
                this.select.value = '';
                this.select.dispatchEvent(new Event('change', { bubbles: true }));
                this.syncFromSelect();
                this.input.value = '';
                this.open(false);
                this.input.focus();
            });

            // مزامنة عند تغيير قيمة القائمة برمجياً
            this.select.addEventListener('change', () => {
                this.syncFromSelect();
            });
        }

        syncFromSelect() {
            const selectedOpt = this.select.selectedOptions[0];
            const hasEmptyOption = Array.from(this.select.options).some(o => o.value === '');

            if (selectedOpt && selectedOpt.value !== '') {
                const parsed = KitchenNameValidator.parseOptionText(selectedOpt.textContent);
                this.input.value = parsed.fullText;
                this.clearBtn.classList.toggle('d-none', !hasEmptyOption);
            } else {
                this.input.value = '';
                this.input.placeholder = this.getPlaceholderText();
                this.clearBtn.classList.add('d-none');
            }
            this.input.classList.remove('is-invalid');
        }

        open(selectAllText = false) {
            if (activeInstance && activeInstance !== this) {
                activeInstance.close();
            }
            activeInstance = this;
            this.isOpen = true;
            this.wrapper.classList.add('ks-open');
            this.portal.classList.remove('d-none');

            if (selectAllText && this.input.value) {
                this.input.select();
                // عند فتح القائمة أول مرة نعرض كل الخيارات المتاحة مع تمييز المختار
                this.renderDropdown('');
            } else {
                this.renderDropdown(this.input.value);
            }

            this.positionPortal();
        }

        close() {
            if (!this.isOpen) return;
            this.isOpen = false;
            this.wrapper.classList.remove('ks-open');
            this.portal.classList.add('d-none');
            if (activeInstance === this) {
                activeInstance = null;
            }
            this.syncFromSelect();
        }

        positionPortal() {
            if (!this.isOpen) return;
            const rect = this.input.getBoundingClientRect();
            const viewportHeight = window.innerHeight;
            const spaceBelow = viewportHeight - rect.bottom;
            const portalMaxHeight = 290;

            this.portal.style.width = `${Math.max(rect.width, 240)}px`;
            this.portal.style.left = `${Math.max(8, Math.min(rect.left, window.innerWidth - Math.max(rect.width, 240) - 8))}px`;

            if (spaceBelow < 200 && rect.top > spaceBelow) {
                const height = Math.min(portalMaxHeight, rect.top - 12);
                this.portal.style.maxHeight = `${height}px`;
                this.portal.style.top = 'auto';
                this.portal.style.bottom = `${viewportHeight - rect.top + 4}px`;
            } else {
                const height = Math.min(portalMaxHeight, Math.max(160, spaceBelow - 12));
                this.portal.style.maxHeight = `${height}px`;
                this.portal.style.bottom = 'auto';
                this.portal.style.top = `${rect.bottom + 4}px`;
            }
        }

        renderDropdown(rawQuery) {
            this.portal.innerHTML = '';
            this.filteredOptions = [];
            this.activeIndex = -1;

            // التحقق الأمني الصارم من أن المدخل اسم فقط وخالٍ من الأوامر والرموز والأرقام
            const validation = KitchenNameValidator.validateSearch(rawQuery);
            if (!validation.valid) {
                this.input.classList.add('is-invalid');
                const alertEl = document.createElement('div');
                alertEl.className = 'ks-security-alert';
                alertEl.innerHTML = `<i class="fa-solid fa-shield-halved mt-1"></i><span>${escapeHtml(validation.error)}</span>`;
                this.portal.appendChild(alertEl);
                this.positionPortal();
                return;
            }

            this.input.classList.remove('is-invalid');
            const normalizedQuery = KitchenNameValidator.normalize(validation.clean);

            // شريط علوي توضيحي داخل القائمة
            const header = document.createElement('div');
            header.className = 'ks-dropdown-header';
            header.innerHTML = `<span><i class="fa-solid fa-magnifying-glass me-1 text-primary"></i> البحث متاح بالاسم فقط</span>`;
            this.portal.appendChild(header);

            const allOptions = Array.from(this.select.options).filter(o => !o.hidden && !o.disabled);
            let currentOptgroup = null;
            let currentGroupHeader = null;

            allOptions.forEach(opt => {
                // خيار التفريغ الأول "-- اختر ... --" يظهر فقط عند عدم كتابة نص بحث
                if (opt.value === '') {
                    if (normalizedQuery) return;
                    const itemEl = document.createElement('div');
                    itemEl.className = 'ks-option text-muted small' + (opt.selected ? ' ks-selected' : '');
                    itemEl.textContent = opt.textContent.trim();
                    itemEl.addEventListener('mousedown', e => {
                        e.preventDefault();
                        this.selectOption(opt);
                    });
                    this.portal.appendChild(itemEl);
                    return;
                }

                const parsed = KitchenNameValidator.parseOptionText(opt.textContent);
                const optDataName = opt.dataset.name ? KitchenNameValidator.normalize(opt.dataset.name) : '';
                const normalizedName = KitchenNameValidator.normalize(parsed.pureName);

                // المطابقة بالاسم فقط (تجاهل الكود أو الرقم عند البحث)
                if (normalizedQuery) {
                    const matchesName = normalizedName.includes(normalizedQuery) || (optDataName && optDataName.includes(normalizedQuery));
                    if (!matchesName) return;
                }

                // دعم مجموعات <optgroup> كما في كتالوج عروض الأسعار
                const parentGroup = opt.parentElement && opt.parentElement.tagName === 'OPTGROUP' ? opt.parentElement : null;
                if (parentGroup && parentGroup !== currentOptgroup) {
                    currentOptgroup = parentGroup;
                    currentGroupHeader = document.createElement('div');
                    currentGroupHeader.className = 'ks-optgroup-label';
                    currentGroupHeader.textContent = parentGroup.label;
                    this.portal.appendChild(currentGroupHeader);
                }

                const itemEl = document.createElement('div');
                itemEl.className = 'ks-option' + (opt.selected ? ' ks-selected' : '');

                const nameSpan = document.createElement('span');
                nameSpan.innerHTML = highlightMatch(parsed.namePart, validation.clean);
                itemEl.appendChild(nameSpan);

                if (parsed.codePart) {
                    const codeSpan = document.createElement('span');
                    codeSpan.className = 'ks-option-code';
                    codeSpan.textContent = parsed.codePart;
                    itemEl.appendChild(codeSpan);
                }

                const entryIndex = this.filteredOptions.length;
                this.filteredOptions.push({ option: opt, element: itemEl });

                if (opt.selected && this.activeIndex === -1) {
                    this.activeIndex = entryIndex;
                }

                itemEl.addEventListener('mouseenter', () => {
                    this.setHighlight(entryIndex);
                });

                itemEl.addEventListener('mousedown', e => {
                    e.preventDefault();
                    this.selectOption(opt);
                });

                this.portal.appendChild(itemEl);
            });

            if (this.filteredOptions.length === 0) {
                const emptyEl = document.createElement('div');
                emptyEl.className = 'ks-empty-state';
                emptyEl.innerHTML = `<i class="fa-solid fa-user-magnifying-glass d-block mb-1 fs-5 opacity-50"></i>لا توجد نتائج مطابقة للاسم المدخل`;
                this.portal.appendChild(emptyEl);
            } else {
                if (this.activeIndex === -1 && normalizedQuery) {
                    this.activeIndex = 0;
                }
                this.setHighlight(this.activeIndex);
            }

            this.positionPortal();
        }

        setHighlight(index) {
            this.filteredOptions.forEach((item, idx) => {
                item.element.classList.toggle('ks-active', idx === index);
            });
            this.activeIndex = index;
        }

        moveHighlight(delta) {
            if (this.filteredOptions.length === 0) return;
            let next = this.activeIndex + delta;
            if (next < 0) next = this.filteredOptions.length - 1;
            if (next >= this.filteredOptions.length) next = 0;
            this.setHighlight(next);
            const targetEl = this.filteredOptions[next]?.element;
            if (targetEl) {
                targetEl.scrollIntoView({ block: 'nearest' });
            }
        }

        selectOption(opt) {
            const changed = this.select.value !== opt.value;
            this.select.value = opt.value;
            this.syncFromSelect();
            this.close();
            if (changed) {
                this.select.dispatchEvent(new Event('change', { bubbles: true }));
            }
        }

        destroy() {
            if (activeInstance === this) {
                activeInstance = null;
            }
            if (this.portal && this.portal.parentNode) {
                this.portal.parentNode.removeChild(this.portal);
            }
            if (this.select && this.select._ksSearchable === this) {
                delete this.select._ksSearchable;
            }
        }
    }

    /**
     * حماية جميع حقول البحث بالاسم (input[name="searchString"]) وحقول الأسماء في الشاشات
     * لمنع تنفيذ أي أمر أو إدخال غير الاسم
     */
    function bindSearchInputsSecurity(root = document) {
        const searchInputs = root.querySelectorAll('input[name="searchString"]');
        searchInputs.forEach(input => {
            if (input.dataset.ksSecured === 'true') return;
            input.dataset.ksSecured = 'true';

            const container = input.closest('.input-group') ? input.closest('.input-group').parentElement : input.parentElement;
            let feedbackEl = container ? container.querySelector('.ks-search-feedback') : null;
            if (!feedbackEl && container) {
                feedbackEl = document.createElement('div');
                feedbackEl.className = 'ks-search-feedback d-none';
                container.appendChild(feedbackEl);
            }

            const checkInput = () => {
                const res = KitchenNameValidator.validateSearch(input.value);
                if (!res.valid) {
                    input.classList.add('is-invalid');
                    if (feedbackEl) {
                        feedbackEl.innerHTML = `<i class="fa-solid fa-shield-halved"></i><span>${escapeHtml(res.error)}</span>`;
                        feedbackEl.classList.remove('d-none');
                    }
                    return false;
                } else {
                    input.classList.remove('is-invalid');
                    if (feedbackEl) {
                        feedbackEl.classList.add('d-none');
                    }
                    return true;
                }
            };

            input.addEventListener('input', checkInput);

            const form = input.closest('form');
            if (form && form.dataset.ksFormSecured !== 'true') {
                form.dataset.ksFormSecured = 'true';
                form.addEventListener('submit', e => {
                    if (!checkInput()) {
                        e.preventDefault();
                        e.stopPropagation();
                        input.focus();
                    }
                });
            }
        });

        // حماية حقول الأسماء الأساسية عند الإدخال في النماذج
        const nameInputs = root.querySelectorAll('input[name="Name"], input[name="ProjectName"], input[name="DisplayName"]');
        nameInputs.forEach(input => {
            if (input.dataset.ksNameSecured === 'true') return;
            input.dataset.ksNameSecured = 'true';

            const isCustomerName = !!input.closest('form[action*="Customers"]');
            const container = input.closest('.input-group') ? input.closest('.input-group').parentElement : input.parentElement;
            let feedbackEl = container ? container.querySelector('.ks-search-feedback') : null;
            if (!feedbackEl && container) {
                feedbackEl = document.createElement('div');
                feedbackEl.className = 'ks-search-feedback d-none';
                container.appendChild(feedbackEl);
            }

            const checkName = () => {
                const res = KitchenNameValidator.validateEntityName(input.value, isCustomerName);
                if (!res.valid) {
                    input.classList.add('is-invalid');
                    if (feedbackEl) {
                        feedbackEl.innerHTML = `<i class="fa-solid fa-shield-halved"></i><span>${escapeHtml(res.error)}</span>`;
                        feedbackEl.classList.remove('d-none');
                    }
                    return false;
                } else {
                    input.classList.remove('is-invalid');
                    if (feedbackEl) {
                        feedbackEl.classList.add('d-none');
                    }
                    return true;
                }
            };

            input.addEventListener('input', checkName);

            const form = input.closest('form');
            if (form) {
                form.addEventListener('submit', e => {
                    if (!checkName()) {
                        e.preventDefault();
                        e.stopPropagation();
                        input.focus();
                    }
                });
            }
        });
    }

    function enhanceAllSelects(root = document) {
        const selects = root.querySelectorAll('select');
        selects.forEach(select => {
            if (shouldEnhanceSelect(select)) {
                new SearchableSelect(select);
            }
        });
    }

    document.addEventListener('DOMContentLoaded', () => {
        enhanceAllSelects(document);
        bindSearchInputsSecurity(document);

        // إغلاق القائمة المنسدلة عند النقر خارجها
        document.addEventListener('mousedown', e => {
            if (!activeInstance) return;
            if (activeInstance.wrapper.contains(e.target) || activeInstance.portal.contains(e.target)) {
                return;
            }
            activeInstance.close();
        });

        // تحديث موقع القائمة عند التمرير أو تغيير حجم النافذة
        window.addEventListener('scroll', () => {
            if (activeInstance) activeInstance.positionPortal();
        }, true);

        window.addEventListener('resize', () => {
            if (activeInstance) activeInstance.positionPortal();
        });

        // مراقبة الصفوف المضافة والمحذوفة ديناميكياً (مثل إضافة سطر خامة أو بند في الفواتير وعروض الأسعار والمخزن)
        const observer = new MutationObserver(mutations => {
            for (const mutation of mutations) {
                mutation.removedNodes.forEach(node => {
                    if (node.nodeType !== Node.ELEMENT_NODE) return;
                    if (node.tagName === 'SELECT') {
                        if (!node.isConnected && node._ksSearchable) node._ksSearchable.destroy();
                    } else if (node.querySelectorAll) {
                        node.querySelectorAll('select').forEach(sel => {
                            if (!sel.isConnected && sel._ksSearchable) sel._ksSearchable.destroy();
                        });
                    }
                });
                mutation.addedNodes.forEach(node => {
                    if (node.nodeType === Node.ELEMENT_NODE) {
                        if (node.tagName === 'SELECT') {
                            if (shouldEnhanceSelect(node)) new SearchableSelect(node);
                        } else if (node.querySelectorAll) {
                            enhanceAllSelects(node);
                            bindSearchInputsSecurity(node);
                        }
                    }
                });
            }
        });

        observer.observe(document.body, { childList: true, subtree: true });
    });
})();
