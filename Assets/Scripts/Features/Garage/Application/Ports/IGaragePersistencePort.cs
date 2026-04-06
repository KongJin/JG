using Features.Garage.Domain;

namespace Features.Garage.Application.Ports
{
    /// <summary>
    /// 편성 데이터 저장/불러오기 포트.
    /// 로컬 JSON 캐시 (보조 목적).
    /// </summary>
    public interface IGaragePersistencePort
    {
        /// <summary>
        /// 편성 데이터를 로컬에 저장.
        /// </summary>
        void Save(GarageRoster roster);

        /// <summary>
        /// 저장된 편성 데이터 불러오기.
        /// 없으면 null 반환.
        /// </summary>
        GarageRoster Load();

        /// <summary>
        /// 저장된 데이터 삭제.
        /// </summary>
        void Delete();
    }
}
