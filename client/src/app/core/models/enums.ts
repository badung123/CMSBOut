export enum StatusActionEnum {
  WaitAccept = 1,
  WaitBank = 2,
  Success = 3,
  Error = 4
}

export const StatusLabels: Record<StatusActionEnum, string> = {
  [StatusActionEnum.WaitAccept]: 'Chờ Duyệt',
  [StatusActionEnum.WaitBank]: 'Chờ Bank',
  [StatusActionEnum.Success]: 'Thành Công',
  [StatusActionEnum.Error]: 'Thất Bại'
};

export const StatusClassMap: Record<StatusActionEnum, string> = {
  [StatusActionEnum.WaitAccept]: 'status-wait-accept',
  [StatusActionEnum.WaitBank]: 'status-wait-bank',
  [StatusActionEnum.Success]: 'status-success',
  [StatusActionEnum.Error]: 'status-error'
};
