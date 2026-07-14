export enum StatusActionEnum {
  WaitAccept = 1,
  WaitBank = 2,
  Success = 3,
  ErrorRequestBank = 4,
  ErrorBank = 5
}

export const StatusLabels: Record<StatusActionEnum, string> = {
  [StatusActionEnum.WaitAccept]: 'Chờ Duyệt',
  [StatusActionEnum.WaitBank]: 'Chờ Bank',
  [StatusActionEnum.Success]: 'Thành Công',
  [StatusActionEnum.ErrorRequestBank]: 'Lỗi Request Bank',
  [StatusActionEnum.ErrorBank]: 'Lỗi Bank'
};

export const StatusClassMap: Record<StatusActionEnum, string> = {
  [StatusActionEnum.WaitAccept]: 'status-wait-accept',
  [StatusActionEnum.WaitBank]: 'status-wait-bank',
  [StatusActionEnum.Success]: 'status-success',
  [StatusActionEnum.ErrorRequestBank]: 'status-error-request',
  [StatusActionEnum.ErrorBank]: 'status-error-bank'
};
