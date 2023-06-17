SET Application_Name TO 'AutoKkutu';

CREATE OR REPLACE FUNCTION __AutoKkutu_Rearrange__(flags INT, endWordFlag INT, attackWordFlag INT, endWordOrdinal INT, attackWordOrdinal INT, normalWordOrdinal INT)
RETURNS INTEGER AS $$
BEGIN
	IF ((flags & endWordFlag) != 0) THEN
		RETURN endWordOrdinal * __MaxWordLength__;
	END IF;
	IF ((flags & attackWordFlag) != 0) THEN
		RETURN attackWordOrdinal * __MaxWordLength__;
	END IF;
	RETURN normalWordOrdinal * __MaxWordLength__;
END;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE FUNCTION __AutoKkutu_RearrangeMission__(word VARCHAR, flags INT, missionword VARCHAR, endWordFlag INT, attackWordFlag INT, endMissionWordOrdinal INT, endWordOrdinal INT, attackMissionWordOrdinal INT, attackWordOrdinal INT, missionWordOrdinal INT, normalWordOrdinal INT)
RETURNS INTEGER AS $$
DECLARE
	occurrence INTEGER;
BEGIN
	occurrence := ROUND((LENGTH(word) - LENGTH(REPLACE(LOWER(word), LOWER(missionWord), ''))) / LENGTH(missionWord));

	IF ((flags & endWordFlag) != 0) THEN
		IF (occurrence > 0) THEN
			RETURN endMissionWordOrdinal * __MaxWordLength__ + occurrence * 256;
		ELSE
			RETURN endWordOrdinal * __MaxWordLength__;
		END IF;
	END IF;
	IF ((flags & attackWordFlag) != 0) THEN
		IF (occurrence > 0) THEN
			RETURN attackMissionWordOrdinal * __MaxWordLength__ + occurrence * 256;
		ELSE
			RETURN attackWordOrdinal * __MaxWordLength__;
		END IF;
	END IF;

	IF occurrence > 0 THEN
		RETURN missionWordOrdinal * __MaxWordLength__ + occurrence * 256;
	ELSE
		RETURN normalWordOrdinal * __MaxWordLength__;
	END IF;
END;
$$ LANGUAGE plpgsql;
