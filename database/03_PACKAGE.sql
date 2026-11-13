-- ============================================================
-- PROJECT: HOTEL BOOKING MANAGEMENT SYSTEM
-- SCHEMA: HOTEL_PROJECT
-- SCRIPT: 03_PACKAGE.sql
-- PURPOSE: PACKAGE FOR HOTEL BOOKING BUSINESS LOGIC
-- ============================================================


SELECT
    OBJECT_NAME,
    OBJECT_TYPE,
    STATUS
FROM USER_OBJECTS
WHERE OBJECT_NAME = 'PKG_HOTEL_BOOKING';

--package
CREATE OR REPLACE PACKAGE PKG_HOTEL_BOOKING AS

    -- 1. Check room availability
    FUNCTION FN_IS_ROOM_AVAILABLE (
        P_ROOM_ID            NUMBER,
        P_CHECK_IN_DATE      DATE,
        P_CHECK_OUT_DATE     DATE,
        P_EXCLUDE_BOOKING_ID NUMBER DEFAULT NULL
    ) RETURN NUMBER;


    -- 2. Calculate total booking amount
    FUNCTION FN_CALCULATE_BOOKING_TOTAL (
        P_BOOKING_ID NUMBER
    ) RETURN NUMBER;


    -- 3. Create booking
    PROCEDURE SP_CREATE_BOOKING (
        P_CUSTOMER_ID    NUMBER,
        P_ROOM_ID        NUMBER,
        P_CHECK_IN_DATE  DATE,
        P_CHECK_OUT_DATE DATE,
        P_GUEST_COUNT    NUMBER,
        P_BOOKING_ID     OUT NUMBER
    );


    -- 4. Confirm booking
    PROCEDURE SP_CONFIRM_BOOKING (
        P_BOOKING_ID NUMBER
    );


    -- 5. Cancel booking
    PROCEDURE SP_CANCEL_BOOKING (
        P_BOOKING_ID NUMBER
    );


    -- 6. Check-in
    PROCEDURE SP_CHECK_IN (
        P_BOOKING_ID NUMBER
    );


    -- 7. Check-out
    PROCEDURE SP_CHECK_OUT (
        P_BOOKING_ID NUMBER
    );


    -- 8. Add service
    PROCEDURE SP_ADD_SERVICE (
        P_BOOKING_ID NUMBER,
        P_SERVICE_ID NUMBER,
        P_QUANTITY   NUMBER
    );

END PKG_HOTEL_BOOKING;


--package body
CREATE OR REPLACE PACKAGE BODY PKG_HOTEL_BOOKING AS


    -- ========================================================
    -- FUNCTION 1: CHECK ROOM AVAILABILITY
    -- Return:
    -- 1 = Available
    -- 0 = Not available
    -- ========================================================
    FUNCTION FN_IS_ROOM_AVAILABLE (
        P_ROOM_ID            NUMBER,
        P_CHECK_IN_DATE      DATE,
        P_CHECK_OUT_DATE     DATE,
        P_EXCLUDE_BOOKING_ID NUMBER
    ) RETURN NUMBER
    IS
        V_ROOM_COUNT    NUMBER;
        V_OVERLAP_COUNT NUMBER;
    BEGIN

        -- Validate date range
        IF P_CHECK_OUT_DATE <= P_CHECK_IN_DATE THEN
            RAISE_APPLICATION_ERROR(
                -20001,
                'Check-out date must be after check-in date.'
            );
        END IF;


        -- Check room exists and is not under maintenance
        SELECT COUNT(*)
        INTO V_ROOM_COUNT
        FROM ROOMS
        WHERE ROOM_ID = P_ROOM_ID
          AND STATUS <> 'MAINTENANCE';


        IF V_ROOM_COUNT = 0 THEN
            RETURN 0;
        END IF;


        -- Check overlapping bookings
        SELECT COUNT(*)
        INTO V_OVERLAP_COUNT
        FROM BOOKINGS
        WHERE ROOM_ID = P_ROOM_ID

          AND BOOKING_STATUS IN (
              'PENDING',
              'CONFIRMED',
              'CHECKED_IN'
          )

          AND P_CHECK_IN_DATE < CHECK_OUT_DATE
          AND P_CHECK_OUT_DATE > CHECK_IN_DATE

          AND (
              P_EXCLUDE_BOOKING_ID IS NULL
              OR BOOKING_ID <> P_EXCLUDE_BOOKING_ID
          );


        IF V_OVERLAP_COUNT = 0 THEN
            RETURN 1;
        ELSE
            RETURN 0;
        END IF;

    END FN_IS_ROOM_AVAILABLE;



    -- ========================================================
    -- FUNCTION 2: CALCULATE BOOKING TOTAL
    -- Room cost + service cost
    -- ========================================================
    FUNCTION FN_CALCULATE_BOOKING_TOTAL (
        P_BOOKING_ID NUMBER
    ) RETURN NUMBER
    IS
        V_ROOM_TOTAL    NUMBER := 0;
        V_SERVICE_TOTAL NUMBER := 0;
    BEGIN

        SELECT
            (CHECK_OUT_DATE - CHECK_IN_DATE) * ROOM_RATE
        INTO V_ROOM_TOTAL
        FROM BOOKINGS
        WHERE BOOKING_ID = P_BOOKING_ID;


        SELECT
            NVL(SUM(QUANTITY * UNIT_PRICE), 0)
        INTO V_SERVICE_TOTAL
        FROM BOOKING_SERVICES
        WHERE BOOKING_ID = P_BOOKING_ID;


        RETURN V_ROOM_TOTAL + V_SERVICE_TOTAL;


    EXCEPTION

        WHEN NO_DATA_FOUND THEN
            RAISE_APPLICATION_ERROR(
                -20002,
                'Booking does not exist.'
            );

    END FN_CALCULATE_BOOKING_TOTAL;



    -- ========================================================
    -- PROCEDURE 1: CREATE BOOKING
    -- ========================================================
    PROCEDURE SP_CREATE_BOOKING (
        P_CUSTOMER_ID    NUMBER,
        P_ROOM_ID        NUMBER,
        P_CHECK_IN_DATE  DATE,
        P_CHECK_OUT_DATE DATE,
        P_GUEST_COUNT    NUMBER,
        P_BOOKING_ID     OUT NUMBER
    )
    IS
        V_ROOM_RATE      NUMBER;
        V_MAX_GUESTS     NUMBER;
        V_ROOM_STATUS    VARCHAR2(20);
        V_CUSTOMER_COUNT NUMBER;
    BEGIN

        -- Validate dates
        IF P_CHECK_OUT_DATE <= P_CHECK_IN_DATE THEN
            RAISE_APPLICATION_ERROR(
                -20003,
                'Check-out date must be after check-in date.'
            );
        END IF;


        IF P_CHECK_IN_DATE < TRUNC(SYSDATE) THEN
            RAISE_APPLICATION_ERROR(
                -20004,
                'Check-in date cannot be in the past.'
            );
        END IF;


        -- Check customer
        SELECT COUNT(*)
        INTO V_CUSTOMER_COUNT
        FROM CUSTOMERS
        WHERE CUSTOMER_ID = P_CUSTOMER_ID
          AND STATUS = 'ACTIVE';


        IF V_CUSTOMER_COUNT = 0 THEN
            RAISE_APPLICATION_ERROR(
                -20005,
                'Customer does not exist or is inactive.'
            );
        END IF;


        -- Get room information
        SELECT
            RT.PRICE_PER_NIGHT,
            RT.MAX_GUESTS,
            R.STATUS
        INTO
            V_ROOM_RATE,
            V_MAX_GUESTS,
            V_ROOM_STATUS
        FROM ROOMS R
        JOIN ROOM_TYPES RT
            ON R.ROOM_TYPE_ID = RT.ROOM_TYPE_ID
        WHERE R.ROOM_ID = P_ROOM_ID;


        -- Room maintenance validation
        IF V_ROOM_STATUS = 'MAINTENANCE' THEN
            RAISE_APPLICATION_ERROR(
                -20006,
                'Room is under maintenance.'
            );
        END IF;


        -- Guest capacity validation
        IF P_GUEST_COUNT <= 0
           OR P_GUEST_COUNT > V_MAX_GUESTS THEN

            RAISE_APPLICATION_ERROR(
                -20007,
                'Guest count exceeds room capacity.'
            );

        END IF;


        -- Check room availability
        IF FN_IS_ROOM_AVAILABLE(
            P_ROOM_ID,
            P_CHECK_IN_DATE,
            P_CHECK_OUT_DATE,
            NULL
        ) = 0 THEN

            RAISE_APPLICATION_ERROR(
                -20008,
                'Room is not available for this period.'
            );

        END IF;


        -- Create booking
        INSERT INTO BOOKINGS (
            CUSTOMER_ID,
            ROOM_ID,
            CHECK_IN_DATE,
            CHECK_OUT_DATE,
            GUEST_COUNT,
            ROOM_RATE,
            BOOKING_STATUS
        )
        VALUES (
            P_CUSTOMER_ID,
            P_ROOM_ID,
            P_CHECK_IN_DATE,
            P_CHECK_OUT_DATE,
            P_GUEST_COUNT,
            V_ROOM_RATE,
            'PENDING'
        )
        RETURNING BOOKING_ID
        INTO P_BOOKING_ID;


    EXCEPTION

        WHEN NO_DATA_FOUND THEN
            RAISE_APPLICATION_ERROR(
                -20009,
                'Room does not exist.'
            );

    END SP_CREATE_BOOKING;



    -- ========================================================
    -- PROCEDURE 2: CONFIRM BOOKING
    -- ========================================================
    PROCEDURE SP_CONFIRM_BOOKING (
        P_BOOKING_ID NUMBER
    )
    IS
        V_STATUS VARCHAR2(20);
    BEGIN

        SELECT BOOKING_STATUS
        INTO V_STATUS
        FROM BOOKINGS
        WHERE BOOKING_ID = P_BOOKING_ID;


        IF V_STATUS <> 'PENDING' THEN

            RAISE_APPLICATION_ERROR(
                -20010,
                'Only pending bookings can be confirmed.'
            );

        END IF;


        UPDATE BOOKINGS
        SET BOOKING_STATUS = 'CONFIRMED',
            UPDATED_AT = SYSTIMESTAMP
        WHERE BOOKING_ID = P_BOOKING_ID;


    EXCEPTION

        WHEN NO_DATA_FOUND THEN
            RAISE_APPLICATION_ERROR(
                -20011,
                'Booking does not exist.'
            );

    END SP_CONFIRM_BOOKING;



    -- ========================================================
    -- PROCEDURE 3: CANCEL BOOKING
    -- ========================================================
    PROCEDURE SP_CANCEL_BOOKING (
        P_BOOKING_ID NUMBER
    )
    IS
        V_STATUS VARCHAR2(20);
    BEGIN

        SELECT BOOKING_STATUS
        INTO V_STATUS
        FROM BOOKINGS
        WHERE BOOKING_ID = P_BOOKING_ID;


        IF V_STATUS NOT IN (
            'PENDING',
            'CONFIRMED'
        ) THEN

            RAISE_APPLICATION_ERROR(
                -20012,
                'This booking cannot be cancelled.'
            );

        END IF;


        UPDATE BOOKINGS
        SET BOOKING_STATUS = 'CANCELLED',
            UPDATED_AT = SYSTIMESTAMP
        WHERE BOOKING_ID = P_BOOKING_ID;


    EXCEPTION

        WHEN NO_DATA_FOUND THEN
            RAISE_APPLICATION_ERROR(
                -20013,
                'Booking does not exist.'
            );

    END SP_CANCEL_BOOKING;



    -- ========================================================
    -- PROCEDURE 4: CHECK-IN
    -- ========================================================
    PROCEDURE SP_CHECK_IN (
        P_BOOKING_ID NUMBER
    )
    IS
        V_STATUS VARCHAR2(20);
    BEGIN

        SELECT BOOKING_STATUS
        INTO V_STATUS
        FROM BOOKINGS
        WHERE BOOKING_ID = P_BOOKING_ID;


        IF V_STATUS <> 'CONFIRMED' THEN

            RAISE_APPLICATION_ERROR(
                -20014,
                'Only confirmed bookings can be checked in.'
            );

        END IF;


        UPDATE BOOKINGS
        SET BOOKING_STATUS = 'CHECKED_IN',
            UPDATED_AT = SYSTIMESTAMP
        WHERE BOOKING_ID = P_BOOKING_ID;


    EXCEPTION

        WHEN NO_DATA_FOUND THEN
            RAISE_APPLICATION_ERROR(
                -20015,
                'Booking does not exist.'
            );

    END SP_CHECK_IN;



    -- ========================================================
    -- PROCEDURE 5: CHECK-OUT
    -- ========================================================
    PROCEDURE SP_CHECK_OUT (
        P_BOOKING_ID NUMBER
    )
    IS
        V_STATUS VARCHAR2(20);
    BEGIN

        SELECT BOOKING_STATUS
        INTO V_STATUS
        FROM BOOKINGS
        WHERE BOOKING_ID = P_BOOKING_ID;


        IF V_STATUS <> 'CHECKED_IN' THEN

            RAISE_APPLICATION_ERROR(
                -20016,
                'Only checked-in bookings can be checked out.'
            );

        END IF;


        UPDATE BOOKINGS
        SET BOOKING_STATUS = 'CHECKED_OUT',
            UPDATED_AT = SYSTIMESTAMP
        WHERE BOOKING_ID = P_BOOKING_ID;


    EXCEPTION

        WHEN NO_DATA_FOUND THEN
            RAISE_APPLICATION_ERROR(
                -20017,
                'Booking does not exist.'
            );

    END SP_CHECK_OUT;



    -- ========================================================
    -- PROCEDURE 6: ADD SERVICE
    -- ========================================================
    PROCEDURE SP_ADD_SERVICE (
        P_BOOKING_ID NUMBER,
        P_SERVICE_ID NUMBER,
        P_QUANTITY   NUMBER
    )
    IS
        V_BOOKING_STATUS VARCHAR2(20);
        V_SERVICE_PRICE  NUMBER;
    BEGIN

        IF P_QUANTITY <= 0 THEN

            RAISE_APPLICATION_ERROR(
                -20018,
                'Quantity must be greater than zero.'
            );

        END IF;


        SELECT BOOKING_STATUS
        INTO V_BOOKING_STATUS
        FROM BOOKINGS
        WHERE BOOKING_ID = P_BOOKING_ID;


        IF V_BOOKING_STATUS <> 'CHECKED_IN' THEN

            RAISE_APPLICATION_ERROR(
                -20019,
                'Services can only be added to checked-in bookings.'
            );

        END IF;


        SELECT UNIT_PRICE
        INTO V_SERVICE_PRICE
        FROM SERVICES
        WHERE SERVICE_ID = P_SERVICE_ID
          AND STATUS = 'ACTIVE';


        INSERT INTO BOOKING_SERVICES (
            BOOKING_ID,
            SERVICE_ID,
            QUANTITY,
            UNIT_PRICE
        )
        VALUES (
            P_BOOKING_ID,
            P_SERVICE_ID,
            P_QUANTITY,
            V_SERVICE_PRICE
        );


    EXCEPTION

        WHEN NO_DATA_FOUND THEN
            RAISE_APPLICATION_ERROR(
                -20020,
                'Booking or active service does not exist.'
            );

    END SP_ADD_SERVICE;


END PKG_HOTEL_BOOKING;


SELECT
    OBJECT_NAME,
    OBJECT_TYPE,
    STATUS
FROM USER_OBJECTS
WHERE OBJECT_NAME = 'PKG_HOTEL_BOOKING'
ORDER BY OBJECT_TYPE;

SELECT PKG_HOTEL_BOOKING.FN_IS_ROOM_AVAILABLE(
    (SELECT ROOM_ID
     FROM ROOMS
     WHERE ROOM_NUMBER = '101'),

    TRUNC(SYSDATE) + 6,
    TRUNC(SYSDATE) + 7,

    NULL
) AS IS_AVAILABLE
FROM DUAL;

SELECT PKG_HOTEL_BOOKING.FN_IS_ROOM_AVAILABLE(
    (SELECT ROOM_ID
     FROM ROOMS
     WHERE ROOM_NUMBER = '101'),

    TRUNC(SYSDATE) + 10,
    TRUNC(SYSDATE) + 12,

    NULL
) AS IS_AVAILABLE
FROM DUAL;


SELECT PKG_HOTEL_BOOKING.FN_IS_ROOM_AVAILABLE(
    (SELECT ROOM_ID
     FROM ROOMS
     WHERE ROOM_NUMBER = '402'),

    TRUNC(SYSDATE) + 5,
    TRUNC(SYSDATE) + 7,

    NULL
) AS IS_AVAILABLE
FROM DUAL;


SELECT
    B.BOOKING_ID,
    R.ROOM_NUMBER,
    B.ROOM_RATE,
    B.CHECK_IN_DATE,
    B.CHECK_OUT_DATE,
    PKG_HOTEL_BOOKING.FN_CALCULATE_BOOKING_TOTAL(
        B.BOOKING_ID
    ) AS TOTAL_AMOUNT
FROM BOOKINGS B
JOIN ROOMS R
    ON B.ROOM_ID = R.ROOM_ID
WHERE R.ROOM_NUMBER = '301'
  AND B.BOOKING_STATUS = 'CHECKED_IN';

SELECT PKG_HOTEL_BOOKING.FN_IS_ROOM_AVAILABLE(
    (SELECT ROOM_ID
     FROM ROOMS
     WHERE ROOM_NUMBER = '103'),

    TRUNC(SYSDATE) + 5,
    TRUNC(SYSDATE) + 3,

    NULL
)
FROM DUAL;


SELECT
    B.BOOKING_ID,
    R.ROOM_NUMBER,
    B.BOOKING_STATUS
FROM BOOKINGS B
JOIN ROOMS R
    ON B.ROOM_ID = R.ROOM_ID
WHERE B.BOOKING_STATUS = 'PENDING';

BEGIN
    PKG_HOTEL_BOOKING.SP_CHECK_IN(11);
END;