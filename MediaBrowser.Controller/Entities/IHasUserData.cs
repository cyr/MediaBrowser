﻿using MediaBrowser.Model.Dto;

namespace MediaBrowser.Controller.Entities
{
    /// <summary>
    /// Interface IHasUserData
    /// </summary>
    public interface IHasUserData : IHasId
    {
        /// <summary>
        /// Gets the user data key.
        /// </summary>
        /// <returns>System.String.</returns>
        string GetUserDataKey();

        /// <summary>
        /// Fills the user data dto values.
        /// </summary>
        /// <param name="dto">The dto.</param>
        /// <param name="userData">The user data.</param>
        /// <param name="user">The user.</param>
        void FillUserDataDtoValues(UserItemDataDto dto, UserItemData userData, User user);
    }
}
