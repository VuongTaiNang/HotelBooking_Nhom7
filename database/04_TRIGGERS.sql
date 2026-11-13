-- ============================================================
-- PROJECT: HOTEL BOOKING MANAGEMENT SYSTEM
-- SCHEMA: HOTEL_PROJECT
-- SCRIPT: 04_TRIGGERS.sql
-- PURPOSE: CORE TRIGGERS FOR BOOKING MANAGEMENT
-- ============================================================

--trigger 1: chống đặt trùng phòng
CREATE OR REPLACE TRIGGER TRG_BOOKING_OVERLAP
FOR INSERT OR UPDATE OF ROOM_ID,
                         CHECK_IN_DATE,
                         CHECK_OUT_DATE,
                         BOOKING_STATUS
ON BOOKINGS
COMPOUND TRIGGER

    TYPE T_BOOKING_REC IS RECORD (
        BOOKING_ID      NUMBER,
        ROOM_ID         NUMBER,
        CHECK_IN_DATE   DATE,
        CHECK_OUT_DATE  DATE,
        BOOKING_STATUS  VARCHAR2(20)
    );

    TYPE T_BOOKING_LIST IS TABLE OF T_BOOKING_REC
        INDEX BY PLS_INTEGER;

    G_BOOKINGS T_BOOKING_LIST;
    G_COUNT     PLS_INTEGER := 0;


    AFTER EACH ROW IS
    BEGIN

        G_COUNT := G_COUNT + 1;

        G_BOOKINGS(G_COUNT).BOOKING_ID :=
            :NEW.BOOKING_ID;

        G_BOOKINGS(G_COUNT).ROOM_ID :=
            :NEW.ROOM_ID;

        G_BOOKINGS(G_COUNT).CHECK_IN_DATE :=
            :NEW.CHECK_IN_DATE;

        G_BOOKINGS(G_COUNT).CHECK_OUT_DATE :=
            :NEW.CHECK_OUT_DATE;

        G_BOOKINGS(G_COUNT).BOOKING_STATUS :=
            :NEW.BOOKING_STATUS;

    END AFTER EACH ROW;


    AFTER STATEMENT IS

        V_OVERLAP_COUNT NUMBER;

    BEGIN

        IF G_COUNT > 0 THEN

            FOR I IN 1 .. G_COUNT LOOP

                IF G_BOOKINGS(I).BOOKING_STATUS IN (
                    'PENDING',
                    'CONFIRMED',
                    'CHECKED_IN'
                ) THEN

                    SELECT COUNT(*)
                    INTO V_OVERLAP_COUNT
                    FROM BOOKINGS B
                    WHERE B.ROOM_ID =
                          G_BOOKINGS(I).ROOM_ID

                      AND B.BOOKING_ID <>
                          G_BOOKINGS(I).BOOKING_ID

                      AND B.BOOKING_STATUS IN (
                          'PENDING',
                          'CONFIRMED',
                          'CHECKED_IN'
                      )

                      AND G_BOOKINGS(I).CHECK_IN_DATE
                          < B.CHECK_OUT_DATE

                      AND G_BOOKINGS(I).CHECK_OUT_DATE
                          > B.CHECK_IN_DATE;


                    IF V_OVERLAP_COUNT > 0 THEN

                        RAISE_APPLICATION_ERROR(
                            -20101,
                            'Room already has an overlapping booking.'
                        );

                    END IF;

                END IF;

            END LOOP;

        END IF;

    END AFTER STATEMENT;

END TRG_BOOKING_OVERLAP;




--trigger 2: audit_booking
CREATE OR REPLACE TRIGGER TRG_BOOKING_AUDIT
AFTER INSERT OR UPDATE OR DELETE
ON BOOKINGS
FOR EACH ROW

BEGIN

    IF INSERTING THEN

        INSERT INTO BOOKING_AUDIT (
            BOOKING_ID,
            ACTION_TYPE,

            OLD_ROOM_ID,
            NEW_ROOM_ID,

            OLD_CHECK_IN_DATE,
            NEW_CHECK_IN_DATE,

            OLD_CHECK_OUT_DATE,
            NEW_CHECK_OUT_DATE,

            OLD_STATUS,
            NEW_STATUS,

            CHANGED_AT,
            CHANGED_BY
        )
        VALUES (
            :NEW.BOOKING_ID,
            'INSERT',

            NULL,
            :NEW.ROOM_ID,

            NULL,
            :NEW.CHECK_IN_DATE,

            NULL,
            :NEW.CHECK_OUT_DATE,

            NULL,
            :NEW.BOOKING_STATUS,

            SYSTIMESTAMP,
            SYS_CONTEXT('USERENV', 'SESSION_USER')
        );


    ELSIF UPDATING THEN

        INSERT INTO BOOKING_AUDIT (
            BOOKING_ID,
            ACTION_TYPE,

            OLD_ROOM_ID,
            NEW_ROOM_ID,

            OLD_CHECK_IN_DATE,
            NEW_CHECK_IN_DATE,

            OLD_CHECK_OUT_DATE,
            NEW_CHECK_OUT_DATE,

            OLD_STATUS,
            NEW_STATUS,

            CHANGED_AT,
            CHANGED_BY
        )
        VALUES (
            :NEW.BOOKING_ID,
            'UPDATE',

            :OLD.ROOM_ID,
            :NEW.ROOM_ID,

            :OLD.CHECK_IN_DATE,
            :NEW.CHECK_IN_DATE,

            :OLD.CHECK_OUT_DATE,
            :NEW.CHECK_OUT_DATE,

            :OLD.BOOKING_STATUS,
            :NEW.BOOKING_STATUS,

            SYSTIMESTAMP,
            SYS_CONTEXT('USERENV', 'SESSION_USER')
        );


    ELSIF DELETING THEN

        INSERT INTO BOOKING_AUDIT (
            BOOKING_ID,
            ACTION_TYPE,

            OLD_ROOM_ID,
            NEW_ROOM_ID,

            OLD_CHECK_IN_DATE,
            NEW_CHECK_IN_DATE,

            OLD_CHECK_OUT_DATE,
            NEW_CHECK_OUT_DATE,

            OLD_STATUS,
            NEW_STATUS,

            CHANGED_AT,
            CHANGED_BY
        )
        VALUES (
            :OLD.BOOKING_ID,
            'DELETE',

            :OLD.ROOM_ID,
            NULL,

            :OLD.CHECK_IN_DATE,
            NULL,

            :OLD.CHECK_OUT_DATE,
            NULL,

            :OLD.BOOKING_STATUS,
            NULL,

            SYSTIMESTAMP,
            SYS_CONTEXT('USERENV', 'SESSION_USER')
        );

    END IF;

END TRG_BOOKING_AUDIT;


--trigger 3: cập nhật trạng thái phòng
CREATE OR REPLACE TRIGGER TRG_BOOKING_ROOM_STATUS
AFTER UPDATE OF BOOKING_STATUS
ON BOOKINGS
FOR EACH ROW

BEGIN

    IF :NEW.BOOKING_STATUS = 'CHECKED_IN'
       AND :OLD.BOOKING_STATUS <> 'CHECKED_IN'
    THEN

        UPDATE ROOMS
        SET STATUS = 'OCCUPIED'
        WHERE ROOM_ID = :NEW.ROOM_ID;


    ELSIF :NEW.BOOKING_STATUS = 'CHECKED_OUT'
          AND :OLD.BOOKING_STATUS <> 'CHECKED_OUT'
    THEN

        UPDATE ROOMS
        SET STATUS = 'CLEANING'
        WHERE ROOM_ID = :NEW.ROOM_ID;

    END IF;

END TRG_BOOKING_ROOM_STATUS;


SELECT
    TRIGGER_NAME,
    TRIGGER_TYPE,
    TRIGGERING_EVENT,
    STATUS
FROM USER_TRIGGERS
WHERE TRIGGER_NAME IN (
    'TRG_BOOKING_OVERLAP',
    'TRG_BOOKING_AUDIT',
    'TRG_BOOKING_ROOM_STATUS'
)
ORDER BY TRIGGER_NAME;


BEGIN
    PKG_HOTEL_BOOKING.SP_CONFIRM_BOOKING(16);
END;

SELECT
    AUDIT_ID,
    BOOKING_ID,
    ACTION_TYPE,
    OLD_STATUS,
    NEW_STATUS,
    CHANGED_AT,
    CHANGED_BY
FROM BOOKING_AUDIT
ORDER BY AUDIT_ID DESC;


SELECT
    B.BOOKING_ID,
    R.ROOM_NUMBER,
    R.STATUS AS ROOM_STATUS,
    B.BOOKING_STATUS
FROM BOOKINGS B
JOIN ROOMS R
    ON B.ROOM_ID = R.ROOM_ID
WHERE B.BOOKING_STATUS = 'CONFIRMED';


BEGIN
    PKG_HOTEL_BOOKING.SP_CHECK_IN(10);
END;

SELECT
    B.BOOKING_ID,
    R.ROOM_NUMBER,
    R.STATUS AS ROOM_STATUS,
    B.BOOKING_STATUS
FROM BOOKINGS B
JOIN ROOMS R
    ON B.ROOM_ID = R.ROOM_ID
WHERE B.BOOKING_ID = 10;

BEGIN
    PKG_HOTEL_BOOKING.SP_CHECK_OUT(10);
END;


SELECT
    B.BOOKING_STATUS,
    R.STATUS AS ROOM_STATUS
FROM BOOKINGS B
JOIN ROOMS R
    ON B.ROOM_ID = R.ROOM_ID
WHERE B.BOOKING_ID = 10;