-- HOTEL_PROJECT / Oracle 23ai
-- Execute as SQL Script once; CREATE OR REPLACE is safe to re-run.
-- Ban booking/ngay nhan phong ngay hom nay neu phong chua AVAILABLE.
-- Dat truoc trong tuong lai van duoc phep neu khong trung booking.
-- Database business guard; applies even when SP_CREATE_BOOKING is called directly.
CREATE OR REPLACE TRIGGER TRG_BOOKING_SAME_DAY_READY
BEFORE INSERT OR UPDATE OF ROOM_ID, CHECK_IN_DATE ON BOOKINGS
FOR EACH ROW
DECLARE
    V_ROOM_STATUS ROOMS.STATUS%TYPE;
    V_TODAY_VN DATE := TRUNC(CAST(SYSTIMESTAMP AT TIME ZONE '+07:00' AS DATE));
BEGIN
    SELECT STATUS INTO V_ROOM_STATUS
    FROM ROOMS
    WHERE ROOM_ID = :NEW.ROOM_ID;

    IF V_ROOM_STATUS = 'MAINTENANCE' THEN
        RAISE_APPLICATION_ERROR(-20125, 'Room is under maintenance and cannot be booked.');
    END IF;

    IF TRUNC(:NEW.CHECK_IN_DATE) <= V_TODAY_VN THEN
        IF V_ROOM_STATUS <> 'AVAILABLE' THEN
            RAISE_APPLICATION_ERROR(-20123,
                'Room is not ready for same-day check-in. Complete cleaning first.');
        END IF;
    END IF;
EXCEPTION
    WHEN NO_DATA_FOUND THEN
        RAISE_APPLICATION_ERROR(-20124, 'Selected room does not exist.');
END TRG_BOOKING_SAME_DAY_READY;
/

-- Verify:
-- SELECT OBJECT_NAME, OBJECT_TYPE, STATUS FROM USER_OBJECTS WHERE OBJECT_NAME='TRG_BOOKING_SAME_DAY_READY';
-- SELECT ROOM_ID, ROOM_NUMBER, STATUS FROM ROOMS WHERE STATUS='CLEANING' ORDER BY ROOM_NUMBER;
