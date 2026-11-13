(() => {
  'use strict';
  const oldInput = document.getElementById('serviceId');
  if (!oldInput) return; // Only runs on Booking Detail with Add Service.

  const form = oldInput.closest('form');
  if (!form) return;
  const status = (document.querySelector('.detail-top .status')?.textContent || '').trim().toUpperCase();
  const finalized = status === 'CANCELLED' || status === 'CHECKED_OUT';

  // The backend/Oracle package remains the authority for every status transition.
  // This merely prevents obvious invalid clicks in the current UI snapshot.
  document.querySelectorAll('.operation-list form').forEach(f => {
    const op = (f.querySelector('input[name="operation"]')?.value || '').toLowerCase();
    const invalid = finalized ||
      (status === 'CONFIRMED' && op === 'confirm') ||
      (status === 'CHECKED_IN' && ['confirm', 'cancel', 'check-in'].includes(op));
    if (invalid) {
      const btn = f.querySelector('button[type="submit"]');
      if (btn) { btn.disabled = true; btn.title = 'Không thể thực hiện thao tác này với trạng thái hiện tại.'; }
    }
  });

  const select = document.createElement('select');
  select.id = oldInput.id;
  select.name = oldInput.name;
  select.required = true;
  select.className = 'hotel-service-select';
  select.setAttribute('aria-label', 'Chọn dịch vụ');
  select.innerHTML = '<option value="">Đang tải danh sách dịch vụ...</option>';
  select.disabled = true;
  oldInput.replaceWith(select);

  const info = document.createElement('small');
  info.className = 'hotel-service-hint';
  select.insertAdjacentElement('afterend', info);

  const btn = form.querySelector('button[type="submit"]');
  if (status !== 'CHECKED_IN') {
    select.innerHTML = '<option value="">Chỉ thêm dịch vụ sau khi Check-in</option>';
    info.textContent = 'Booking hiện tại: ' + status + '. Chỉ thêm dịch vụ khi đã CHECKED_IN.';
    if (btn) btn.disabled = true;
    return;
  }

  const fmt = new Intl.NumberFormat('vi-VN', { maximumFractionDigits: 0 });
  fetch('/services/list', { headers: { Accept: 'application/json' }, credentials: 'same-origin' })
    .then(async response => {
      if (!response.ok) throw new Error('HTTP ' + response.status);
      return await response.json();
    })
    .then(items => {
      select.innerHTML = '';
      const placeholder = document.createElement('option');
      placeholder.value = '';
      placeholder.textContent = '— Chọn dịch vụ —';
      select.append(placeholder);
      if (!Array.isArray(items) || items.length === 0) {
        placeholder.textContent = 'Không có dịch vụ trong hệ thống';
        info.textContent = 'Kiểm tra dữ liệu bảng SERVICES.';
        if (btn) btn.disabled = true;
        return;
      }
      for (const item of items) {
        const option = document.createElement('option');
        option.value = String(item.serviceId);
        const hasPrice = item.price !== null && item.price !== undefined;
        option.textContent = `${item.serviceName}${hasPrice ? ` — ${fmt.format(Number(item.price))} đ` : ''}`;
        select.append(option);
      }
      select.disabled = false;
      info.textContent = 'Tên và giá dịch vụ được truy vấn trực tiếp từ Oracle.';
    })
    .catch(error => {
      console.error('Loading services failed:', error);
      select.innerHTML = '<option value="">Không thể tải dịch vụ</option>';
      info.textContent = 'Kiểm tra https://localhost:7083/api/services và cấu hình kết nối Backend.';
      if (btn) btn.disabled = true;
    });
})();
