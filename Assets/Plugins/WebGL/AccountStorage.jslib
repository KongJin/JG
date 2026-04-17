var AccountStoragePlugin = {

    AccountStorage_SetItem: function (keyPtr, valuePtr) {
        try {
            var key = UTF8ToString(keyPtr);
            var value = UTF8ToString(valuePtr);
            window.localStorage.setItem(key, value);
        } catch (error) {
            console.warn("[AccountStorage] setItem failed:", error);
        }
    },

    AccountStorage_GetItem: function (keyPtr) {
        try {
            var key = UTF8ToString(keyPtr);
            var value = window.localStorage.getItem(key);
            if (value === null || value === undefined) {
                return 0;
            }

            return stringToNewUTF8(value);
        } catch (error) {
            console.warn("[AccountStorage] getItem failed:", error);
            return 0;
        }
    },

    AccountStorage_RemoveItem: function (keyPtr) {
        try {
            var key = UTF8ToString(keyPtr);
            window.localStorage.removeItem(key);
        } catch (error) {
            console.warn("[AccountStorage] removeItem failed:", error);
        }
    }
};

mergeInto(LibraryManager.library, AccountStoragePlugin);
