namespace AionDpsMeter.Services.Models
{
    internal enum PacketTypeEnum
    {
        P_04_38,
        P_FF_FF,
        UNKNOWN,
        BROKEN
    }

    internal enum PacketProcessResult
    {
        WRONG_LENTH_BYTE,
        HEADER_ERROR,
        TARGET_ID_ERROR,
        SWITH_VALUE_ERROR,
        FLAG_ERROR,
        ACTOR_ID_ERROR,
        SKILLCODE_ERROR,
        DMG_TYPE_ERROR,
        SPECIAL_FLAGS_ERROR,
        UNKNOWN_FIELD_ERROR,
        DAMAGE_ERROR,
        ACTORID_EQUALS_TARGETID,
        UNKNOWN_ERROR,
        SUCCES
    }
}
