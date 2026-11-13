(() => {
  'use strict';
  // Dismissible notices with timer; errors get a longer reading interval.
  document.querySelectorAll('.notice.success, .notice.error, .hk-notice.hk-success, .hk-notice.hk-error').forEach(box => {
    if (box.dataset.polishedNotice === '1') return;
    box.dataset.polishedNotice = '1';
    box.classList.add('hotel-notice');
    const isError = box.classList.contains('error') || box.classList.contains('hk-error');
    box.setAttribute('role', isError ? 'alert' : 'status');
    const close = document.createElement('button');
    close.type = 'button';
    close.className = 'hotel-notice-close';
    close.textContent = '×';
    close.setAttribute('aria-label', 'Đóng thông báo');
    box.append(close);
    let timer;
    const dismiss = () => {
      window.clearTimeout(timer);
      box.classList.add('hotel-notice-leaving');
      window.setTimeout(() => box.remove(), 290);
    };
    const schedule = () => { timer = window.setTimeout(dismiss, isError ? 11000 : 6000); };
    close.addEventListener('click', dismiss);
    box.addEventListener('mouseenter', () => window.clearTimeout(timer));
    box.addEventListener('mouseleave', schedule);
    box.addEventListener('focusin', () => window.clearTimeout(timer));
    box.addEventListener('focusout', schedule);
    schedule();
  });

  // A second UI guard to reduce invalid actions when services-dropdown.js is cached.
  const status = (document.querySelector('.detail-top .status')?.textContent || '').trim().toUpperCase();
  if (status) {
    const allowed = {
      PENDING: new Set(['confirm', 'cancel']),
      CONFIRMED: new Set(['check-in', 'cancel']),
      CHECKED_IN: new Set(['check-out']),
      CHECKED_OUT: new Set(),
      CANCELLED: new Set()
    };
    document.querySelectorAll('.operation-list form').forEach(f => {
      const op = f.querySelector('input[name="operation"]')?.value?.toLowerCase();
      if (!op || !allowed[status] || allowed[status].has(op)) return;
      const btn = f.querySelector('button[type="submit"]');
      if (btn) {
        btn.disabled = true;
        btn.title = 'Thao tác không phù hợp với trạng thái booking hiện tại.';
      }
    });
    const serviceForm = document.querySelector('.service-form');
    if (serviceForm && status !== 'CHECKED_IN') {
      const select = serviceForm.querySelector('select[name="serviceId"],input[name="serviceId"]');
      const submit = serviceForm.querySelector('button[type="submit"]');
      if (select) select.disabled = true;
      if (submit) {
        submit.disabled = true;
        submit.title = 'Chỉ thêm dịch vụ khi khách đã Check-in.';
      }
      const hint = serviceForm.querySelector('.hotel-service-hint');
      if (hint) hint.textContent = 'Chỉ thêm dịch vụ cho booking đã Check-in.';
    }
  }
})();
