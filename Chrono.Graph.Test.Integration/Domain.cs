using Chrono.Graph.Core.Constant;
using Chrono.Graph.Notations;
using System.Numerics;

namespace Chrono.Graph.Test.Integration
{
    public enum EdgeLabeledEnum
    {
        [GraphEdge("INTERPLANETARY")]
        typeA,
        [GraphEdge("ORBITAL")]
        type1
    }

    public enum SimpleEnum
    {
        fish,
        color
    }
    public enum NormalEnum
    {
        tree,
        mission,
        brown,
    }
    public enum UserRole
    {
        Supervisor,
        Operator,
        Facilitator,
        Spotter,
        Official,
        Marshal
    }

    public enum ContactCategory
    {
        [GraphEdge("TEAM_LIAISON")]
        TeamLiaison,
        [GraphEdge("OFFICE_PERSON")]
        OfficeSupport,
        [GraphEdge("CLIQUE_MANAGER")]
        PartnerRelations,
        [GraphEdge("SHIPPING_BOSS")]
        PortageAuthority,
        [GraphEdge("HELPER_LINE")]
        HelperLine
    }
    public class Base
    {

        public string Id { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? ModifiedAt { get; set; }
        public string? Notes { get; set; }
    }
    public class Stuff
    {
        public Stuff() { }
        public Stuff(string wwwhhhaaat) => what = wwwhhhaaat;
        [GraphIdentifier]
        public string what { get; set; } = "cherries";
    }
    public class HowDictionaryEdgesWork
    {

        [GraphKeyLabelling] //this enables the key as an edge label
        public Dictionary<EdgeLabeledEnum, Stuff> EdgeValStuff { get; set; } = new Dictionary<EdgeLabeledEnum, Stuff>{ 
            { EdgeLabeledEnum.type1, new Stuff("One o-clock dog") },
            { EdgeLabeledEnum.typeA, new Stuff() } 
        };
        [GraphKeyLabelling]
        public Dictionary<SimpleEnum, Stuff> EdgeDictStuff { get; set; } = new Dictionary<SimpleEnum, Stuff>
        {
            { SimpleEnum.fish, new Stuff("carp") },
            { SimpleEnum.color, new Stuff("zebra") }
        };
        public Dictionary<NormalEnum, Stuff> NormalStuff { get; set; } = new Dictionary<NormalEnum, Stuff>
        {
            { NormalEnum.brown, new Stuff("frodo") },
            { NormalEnum.mission, new Stuff("beechnut") },
            { NormalEnum.tree, new Stuff("sicamore") }
        };
        public string Word { get; set; } = "hampster";
        [GraphIdentifier]
        public int Number { get; set; } = 5;
    }

    public class CommunicationDetail : Base
    {
        public string ContactNumber { get; set; } = string.Empty;
        public DateTime? VerifiedAt { get; set; }
        public int CountryPrefix { get; set; }
        public bool CanText { get; set; }
        public List<AlertPreference> AlertSettings { get; set; } = new();
    }

    public class AlertPreference : Base
    {
        public string Tag { get; set; } = string.Empty;
        public string? Description { get; set; }
    }

    public class IndividualProfile : Base
    {
        public string First { get; set; } = string.Empty;
        public string? Last { get; set; }
    }

    public class UserIdentity : Base
    {
        public IndividualProfile? ProfileData { get; set; }
        public string EmailAddress { get; set; } = string.Empty;
    }

    public class FacilityUnit : Base
    {
        public string Title { get; set; } = "Fancyville";
        public Address Address { get; set; } = new();
    }

    public class PaymentCredential : Base
    {
        public string CardOwner { get; set; } = string.Empty;
        public string? CardDigits { get; set; }
        public int SecurityCode { get; set; }
    }


    public class EntityHub : Base
    {
        public FacilityUnit Headquarters { get; set; } = new();
        public List<FacilityUnit> Locations { get; set; } = [];
        public PaymentCredential? FinancialSource { get; set; }
        [GraphKeyLabelling]
        public Dictionary<UserRole, IList<UserIdentity>> Permissions { get; set; } = [];
        [GraphKeyLabelling]
        public Dictionary<ContactCategory, CommunicationDetail> ContactPoints { get; set; } = [];
    }

    public class Location : Base
    {
        public float? Latitude { get; set; }
        public float? Longitude { get; set; }
        public string? Description { get; set; }
    }
    public class DistinctLocation : Location
    {
        [GraphIdentifier]
        public string? Name { get; set; }
    }
    public class Address : Location
    {

        public string Addressee { get; set; } = string.Empty;
        public string Street1 { get; set; } = string.Empty;
        public string? Street2 { get; set; }
        public string? Street3 { get; set; }
        [GraphEdge("Township")]
        public Township? City { get; set; }
        public Province? Province { get; set; }
        public ProvincialCode? ProvincialCode { get; set; }
        public Country? Country { get; set; }

        [GraphObjectSerialize]
        public Vector2? GeoLocation { get; set; }
        [GraphIdentifier]
        public string AddressComposite {
            get {
                var street2 = !string.IsNullOrEmpty(Street2) ? $" {Street2}" : "";
                var street3 = !string.IsNullOrEmpty(Street3) ? $" {Street3}" : "";
                var city = !string.IsNullOrEmpty(City?.Name) ? $" {City?.Name}" : "";
                var province = !string.IsNullOrEmpty(Province?.Name)
                    ? $" {Province.Name}"
                    : !string.IsNullOrEmpty(City?.Province?.Name)
                        ? $" {City.Province.Name}"
                        : "";
                var provincialCode = !string.IsNullOrEmpty(ProvincialCode?.Code) ? $" {ProvincialCode.Code}" : "";
                var country = !string.IsNullOrEmpty(Country?.Name)
                    ? $" {Country.Name}"
                    : !string.IsNullOrEmpty(City?.Country?.Name)
                        ? $" {City.Country.Name}"
                        : !string.IsNullOrEmpty(Province?.Country?.Name)
                            ? $" {Province.Country.Name}"
                            : !string.IsNullOrEmpty(ProvincialCode?.Country?.Name)
                                ? $" {ProvincialCode.Country.Name}"
                                : "";
                return $"{Street1}{street2}{street3}{city}{province}{provincialCode}{country}";
            }
        }
    }
    public class Township : DistinctLocation
    {
        public Province? Province { get; set; }
        public IList<ProvincialCode>? ProvincialCodes { get; set; }
        public Country? Country { get; set; }
    }
    public class Province : DistinctLocation
    {
        public IList<Township>? Townships { get; set; }
        public Country? Country { get; set; }
    }
    public class Country : DistinctLocation
    {
        public IList<Province>? Provinces { get; set; }

    }
    public class ProvincialCode : Location
    {
        [GraphIdentifier]
        public string Code { get; set; } = string.Empty;
        public IList<Township> Townships { get; set; } = [];
        public IList<Province> Provinces { get; set; } = [];
        public Country? Country { get; set; }
    }
    [GraphLabel("BigChicken")]
    public class NodeBase : Base
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool? IsSeries { get; set; }
        public float[]? FloatBasket { get; set; }
        [GraphLabel("DateOfBirth")]
        public DateTime? DOB { get; set; }
        [GraphEdge("RULES", GraphEdgeDirection.Out)]
        public NodeSubclass? SubClass { get; set; }
        [GraphLabel("HomeTown")]
        [GraphObjectSerialize]
        public NodeSubclass? JsonClass { get; set; }
        public int? CowsMistakenForChicken { get; set; }
        public float? StrutCount { get; set; }
        public float? HitPercentage { get; set; }
        public bool[]? BoolBasket { get; set; }
        public List<bool>? BohemianCrapsidy { get; set; }
        public string[]? StringBasket { get; set; }
        public HashSet<string>? StringParty { get; set; }
        public List<float>? FloatFrenchbraid { get; set; }
    }

    [GraphLabel("ChickenCoop")]
    public class NodeSubclass : Base
    {

        public string? Address { get; set; }
        public string? Color { get; set; }
        public bool? Underground { get; set; }
        public string[]? StringBasket { get; set; }
        public IEnumerable<string>? StringParty { get; set; }
        public bool[]? BoolBasket { get; set; }
        public IEnumerable<bool>? BohemianCrapsidy { get; set; }

    }
}