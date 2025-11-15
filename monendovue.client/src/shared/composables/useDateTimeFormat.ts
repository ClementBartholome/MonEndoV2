import { format as dateFnsFormat, parseISO } from 'date-fns';

export function useDateTimeFormat() {
  const formatDateDisplay = (date: Date | string): string => {
    const dateObj = typeof date === 'string' ? new Date(date) : date;
    return dateFnsFormat(dateObj, 'dd-MM-yyyy').replace(/-/g, '/');
  };

  const formatDateInput = (date: Date | string): string => {
    const dateObj = typeof date === 'string' ? new Date(date) : date;
    return dateFnsFormat(dateObj, 'yyyy-MM-dd');
  };

  const formatTimeInput = (date: Date | string): string => {
    const dateObj = typeof date === 'string' ? new Date(date) : date;
    return dateFnsFormat(dateObj, 'HH:mm');
  };

  const formatTimeDisplay = (time: Date | string): string => {
    if (typeof time === 'string') {
      if (time.includes('h') && !time.includes('T')) {
        return time;
      }

      if (/^\d{2}:\d{2}$/.test(time)) {
        return time.replace(':', 'h');
      }

      const dateObj = new Date(time);
      if (isNaN(dateObj.getTime())) {
        return time;
      }
      const hours = dateObj.getHours().toString().padStart(2, '0');
      const minutes = dateObj.getMinutes().toString().padStart(2, '0');
      return `${hours}h${minutes}`;
    }

    const hours = time.getHours().toString().padStart(2, '0');
    const minutes = time.getMinutes().toString().padStart(2, '0');
    return `${hours}h${minutes}`;
  };

  const extractTimeDisplay = (date: Date): string => {
    const hours = date.getHours().toString().padStart(2, '0');
    const minutes = date.getMinutes().toString().padStart(2, '0');
    return `${hours}h${minutes}`;
  };

  const combineDateTime = (dateStr: string, timeStr: string): Date => {
    const dateTimeString = `${dateStr}T${timeStr}`;
    const dateTime = new Date(dateTimeString);
    const timezoneOffset = dateTime.getTimezoneOffset() * 60 * 1000;
    return new Date(dateTime.getTime() - timezoneOffset);
  };

  const parseDate = (isoString: string): Date => {
    return parseISO(isoString);
  };

  const formatISODateForInput = (isoString: string | Date): string => {
    if (typeof isoString === 'string') {
      return dateFnsFormat(parseISO(isoString), 'yyyy-MM-dd');
    }
    return dateFnsFormat(isoString, 'yyyy-MM-dd');
  };

  const getCurrentDateInput = (): string => {
    return dateFnsFormat(new Date(), 'yyyy-MM-dd');
  };

  const getCurrentTimeInput = (): string => {
    return dateFnsFormat(new Date(), 'HH:mm');
  };

  const getCurrentMonthYear = (): string => {
    return dateFnsFormat(new Date(), 'yyyy-MM');
  };

  return {
    formatDateDisplay,
    formatDateInput,
    formatTimeInput,
    formatTimeDisplay,
    extractTimeDisplay,
    combineDateTime,
    parseDate,
    formatISODateForInput,
    getCurrentDateInput,
    getCurrentTimeInput,
    getCurrentMonthYear,
  };
}
