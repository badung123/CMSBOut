export function toUppercaseNoAccent(input: string): string {
  if (!input) return '';
  return input
    .normalize('NFD')
    .replace(/[\u0300-\u036f]/g, '')
    .replace(/đ/g, 'd')
    .replace(/Đ/g, 'D')
    .toUpperCase();
}

export function formatCurrency(value: number): string {
  return new Intl.NumberFormat('vi-VN').format(value);
}

export function formatDateTime(value: string | null): string {
  if (!value) return '-';
  return new Date(value).toLocaleString('vi-VN');
}
