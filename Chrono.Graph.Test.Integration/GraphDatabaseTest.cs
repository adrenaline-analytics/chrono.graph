using AutoFixture;
using Chrono.Graph.Adapter.Neo4j;
using Chrono.Graph.Core.Application;
using Chrono.Graph.Core.Constant;
using Chrono.Graph.Core.Utilities;
using Chrono.Graph.Notations;
using Neo4j.Driver;
using System.Diagnostics;

namespace Chrono.Graph.Test.Integration
{
    public class GraphDatabaseTest : BaseTest
    {
        public GraphDatabaseTest() : base() { }

        [Fact]
        public async Task OrganizationFacilitiesAndHeadquartersSaveOkay()
        {
            var id = Id();
            var timestamp = DateTime.UtcNow;
            var notes = $"test-{id}-{timestamp}";
            var locationA = new FacilityUnit
            {
                Id = Id(),
                Title = "Facility A",
                Notes = notes,
                CreatedAt = timestamp,
                ModifiedAt = timestamp,
            }; 

            var org = new EntityHub
            {
                Id = id,
                Notes = notes,
                CreatedAt = timestamp,
                ModifiedAt = timestamp,
                Headquarters = locationA,
                Locations = new List<FacilityUnit> { 
                    locationA
                },
            };

            await _db.Post(org);
            var result = await _db.GetScalar<EntityHub>(
                q => q.Where<EntityHub, string>(o => o.Id, Is.Equal(id)),
                j => j.Join<EntityHub, IList<FacilityUnit>>(o => o.Locations)
                    .Join<EntityHub, FacilityUnit>(o => o.Headquarters)
                );


            Assert.NotNull(result);
            Assert.NotNull(result.Locations);
            Assert.NotNull(result.Headquarters);
            Assert.NotEmpty(result.Locations);
            Assert.Equal(id, result.Id);
            Assert.Equal(org.Locations.Count, result.Locations.Count);
            Assert.Equal(org.Locations.First().Title, result.Locations.First().Title);
            Assert.Equal(org.Locations.First().Notes, result.Locations.First().Notes);
            Assert.Equal(org.Headquarters.Title, result.Headquarters.Title);
            Assert.Equal(org.Headquarters.Notes, result.Headquarters.Notes);

            var newName = "Rename";
            result.Headquarters.Title = newName;

            await _db.Patch(result);

            var newresult = await _db.GetScalar<EntityHub>(
                q => q.Where<EntityHub, string>(o => o.Id, Is.Equal(id)),
                j => j.Join<EntityHub, IList<FacilityUnit>>(o => o.Locations)
                    .Join<EntityHub, FacilityUnit>(o => o.Headquarters)
                );

            Assert.NotNull(newresult);
            Assert.NotNull(newresult.Locations);
            Assert.NotNull(newresult.Headquarters);
            Assert.NotEmpty(newresult.Locations);
            Assert.Equal(id, newresult.Id);
            Assert.Equal(result.Locations.Count, newresult.Locations.Count);
            Assert.Equal(newName, newresult.Locations.First().Title);
            Assert.Equal(newName, newresult.Headquarters.Title);

        }
        [Fact]
        public async Task WhenOneObjectIsRetrievedMultipleTimesUseOneInstanceForAll()
        {
            var id = Id();
            var timestamp = DateTime.UtcNow;
            var notes = $"test-{id}-{timestamp}";
            
            var facilityA = new FacilityUnit
            {
                Id = Id(),
                Title = "Facility A",
                Notes = notes,
                CreatedAt = timestamp,
                ModifiedAt = timestamp,
            };

            var entity = new EntityHub
            {
                Id = id,
                Notes = notes,
                CreatedAt = timestamp,
                ModifiedAt = timestamp,
                Headquarters = facilityA,
                Locations = new List<FacilityUnit> { facilityA },
            };

            await _db.Post(entity);
            var result = await _db.GetScalar<EntityHub>(
                q => q.Where<EntityHub, string>(o => o.Id, Is.Equal(id)),
                j => j.Join<EntityHub, IList<FacilityUnit>>(o => o.Locations)
                    .Join<EntityHub, FacilityUnit>(o => o.Headquarters)
            );

            Assert.NotNull(result);
            Assert.NotNull(result.Locations);
            Assert.NotNull(result.Headquarters);
            Assert.Single(result.Locations);
            Assert.Equal(result.Headquarters, result.Locations.First());
            result.Headquarters.Title = "adskjhfkalsufih322";
            Assert.Equal(result.Headquarters.Title, result.Locations.First().Title);
        }
        [Fact]
        public async Task OrganizationRoleAccessSavesWithKeyLabelling()
        {
            var id = Id();
            var phone = "888coolguy";
            var email = "888@cool.guy";
            var timestamp = DateTime.UtcNow;
            var notes = $"test-{id}-{timestamp}";
            var addressHq = TestUtils.MakeAddress();
            
            var liaisonContact = new CommunicationDetail
            {
                Id = Id(),
                ContactNumber = "605 Liaison Contact lol",
                CreatedAt = timestamp,
                ModifiedAt = timestamp,
                Notes = notes,
                VerifiedAt = timestamp,
                CountryPrefix = 1,
                CanText = true,
                AlertSettings = new List<AlertPreference>
                {
                    new AlertPreference
                    {
                        Id = Id(),
                        CreatedAt = timestamp,
                        ModifiedAt = timestamp,
                        Notes = notes,
                        Tag = "All notifications",
                        Description = "Could you describe the ruckus, sir?"
                    }
                }
            };

            var chipmunk = new UserIdentity
            {
                Id = Id(),
                ProfileData = new IndividualProfile
                {
                    Id = Id(),
                    First = "Chipmunk",
                    Last = "Habertonfaber",
                    Notes = notes,
                    CreatedAt = timestamp,
                    ModifiedAt = timestamp,
                },
                EmailAddress = email,
                Notes = notes,
                CreatedAt = timestamp,
                ModifiedAt = timestamp,
            };

            var facilityA = new FacilityUnit
            {
                Id = Id(),
                Title = "Facility A",
                Address = addressHq,
            };

            var entity = new EntityHub
            {
                Id = id,
                Notes = notes,
                CreatedAt = timestamp,
                ModifiedAt = timestamp,
                Headquarters = facilityA,
                Locations = new List<FacilityUnit> { facilityA },
                FinancialSource = new PaymentCredential
                {
                    Id = Id(),
                    CardOwner = "Chris King",
                    CardDigits = "1422024911924101",
                    SecurityCode = 114,
                    Notes = notes,
                    CreatedAt = timestamp,
                    ModifiedAt = timestamp,
                },
                Permissions = new Dictionary<UserRole, IList<UserIdentity>>
                {
                    {
                        UserRole.Supervisor, new List<UserIdentity>
                        {
                            chipmunk,
                            new UserIdentity
                            {
                                Id = Id(),
                                ProfileData = new IndividualProfile
                                {
                                    Id = Id(),
                                    First = "Beau",
                                    Last = "Wilson",
                                    Notes = notes,
                                    CreatedAt = timestamp,
                                    ModifiedAt = timestamp,
                                },
                                EmailAddress = phone,
                                Notes = notes,
                                CreatedAt = timestamp,
                                ModifiedAt = timestamp,
                            }
                        }
                    },
                    {
                        UserRole.Operator, new List<UserIdentity>
                        {
                            chipmunk,
                            new UserIdentity
                            {
                                Id = Id(),
                                ProfileData = new IndividualProfile
                                {
                                    Id = Id(),
                                    First = "Calipso",
                                    Notes = notes,
                                    CreatedAt = timestamp,
                                    ModifiedAt = timestamp,
                                },
                                EmailAddress = "miss@terio.us",
                                Notes = notes,
                                CreatedAt = timestamp,
                                ModifiedAt = timestamp,
                            }
                        }
                    }
                },
                ContactPoints = new Dictionary<ContactCategory, CommunicationDetail>
                {
                    { ContactCategory.TeamLiaison, liaisonContact },
                    { ContactCategory.OfficeSupport, liaisonContact }
                }
            };

            await _db.Post(entity);
            var result = await _db.GetScalar<EntityHub>(
                q => q.Where<EntityHub, string>(o => o.Id, Is.Equal(id)),
                j => j.Join<EntityHub, Dictionary<UserRole, IList<UserIdentity>>>(o => o.Permissions)
                    .Join<EntityHub, IList<FacilityUnit>>(o => o.Locations)
                    .Join<EntityHub, PaymentCredential>(o => o.FinancialSource)
                    .Join<EntityHub, FacilityUnit>(o => o.Headquarters)
                    .Join<EntityHub, Dictionary<ContactCategory, CommunicationDetail>>(o => o.ContactPoints,
                        contactField => contactField.JoinOptional<CommunicationDetail, IList<AlertPreference>>(c => c.AlertSettings))
            );

            Assert.NotNull(result);
            Assert.Equal(id, result.Id);
            Assert.Equal(entity.Permissions.Count, result.Permissions.Count);
            Assert.Equal(entity.Permissions[UserRole.Supervisor].First().EmailAddress, result.Permissions[UserRole.Supervisor].First().EmailAddress);
            Assert.NotNull(result.ContactPoints);
            Assert.Equal(entity.ContactPoints[ContactCategory.TeamLiaison].Id, result.ContactPoints[ContactCategory.TeamLiaison].Id);
            Assert.NotNull(result.ContactPoints[ContactCategory.TeamLiaison].AlertSettings);
            Assert.Equal(result.ContactPoints[ContactCategory.TeamLiaison].AlertSettings.First().Tag, entity.ContactPoints[ContactCategory.TeamLiaison].AlertSettings.First().Tag);

            result.ContactPoints[ContactCategory.PartnerRelations] = liaisonContact;
            await _db.Put(result);

            var updatedResult = await _db.GetScalar<EntityHub>(
                q => q.Where<EntityHub, string>(o => o.Id, Is.Equal(id)),
                j => j.Join<EntityHub, Dictionary<ContactCategory, CommunicationDetail>>(
                    o => o.ContactPoints, j2 => j2.JoinOptional<CommunicationDetail, IList<AlertPreference>>(c => c.AlertSettings)));

            Assert.NotNull(updatedResult);
            Assert.NotNull(updatedResult.ContactPoints);
            Assert.Equal(result.ContactPoints[ContactCategory.TeamLiaison].Id, updatedResult.ContactPoints[ContactCategory.TeamLiaison].Id);
            Assert.NotNull(updatedResult.ContactPoints[ContactCategory.TeamLiaison].AlertSettings);
            Assert.Equal(result.ContactPoints[ContactCategory.PartnerRelations].Id, updatedResult.ContactPoints[ContactCategory.PartnerRelations].Id);
            Assert.NotNull(updatedResult.ContactPoints[ContactCategory.PartnerRelations].AlertSettings);

        }

        [Fact]
        public async Task PostDictionaryEdgeDefinitionsWithCustomLabels()
        {
            var thing = new HowDictionaryEdgesWork();
            await _db.Post(thing, 3);
            var oThing = await _db.Get<HowDictionaryEdgesWork>(q => q.Where<HowDictionaryEdgesWork, string>(t => t.Word, Is.Equal(thing.Word)), q =>
            {
                q.Join<HowDictionaryEdgesWork, Dictionary<EdgeLabeledEnum, Stuff>>(t => t.EdgeValStuff);
                q.Join<HowDictionaryEdgesWork, Dictionary<SimpleEnum, Stuff>>(t => t.EdgeDictStuff);
                q.Join<HowDictionaryEdgesWork, Dictionary<NormalEnum, Stuff>>(t => t.NormalStuff);
            });
            Assert.NotNull(oThing.FirstOrDefault());
            Assert.Single(oThing);
            Assert.Equal(thing.Word, oThing.First().Word);
            Assert.All(oThing.First().NormalStuff, s => { });
        }


        [Fact]
        public async Task ObjectCanAddOneChildObjectWithTwoDifferentEdges()
        {
            var id = Id();
            var address = TestUtils.MakeAddress();
            var hq = new FacilityUnit
            {
                Id = Id(),
                Address = address
            };
            var org = new EntityHub
            {
                Id = id,
                Headquarters = hq,
                Locations = [hq]
            };
            await _db.Put(org);
        }
        [Fact]
        public async Task AddressesAreAppliedToOrgsCorrectlyWhenFacilitiesAreGiven()
        {
            var id = Id();
            var addressHq = TestUtils.MakeAddress();
            var hq = new FacilityUnit
            {
                Id = Id(),
                Address = addressHq
            };
            var spotTwo = new FacilityUnit
            {
                Id = Id(),
                Address = TestUtils.MakeAddress()
            };
            var org = new EntityHub
            {
                Id = id,
                Headquarters = hq,
                Locations = [hq, spotTwo]
                
            };
            await _db.Put(org, 5);

        }
        [Fact]
        public async Task AddressChildObjectsFullyEdgeTogether()
        {
            var address = TestUtils.MakeAddress();
            await _db.Put(address, 15);

        }
        [Fact]
        public async Task RaceRepositoryIsAbleToStoreAndDeleteARace()
        {
            var id = Id();
            var node = _fixture.Create<NodeBase>();
            node.Id = id;
            node.Notes = id;
            await _db.Put(node);
            var savedNode = await _db.GetScalar<NodeBase>(q => q.Where<NodeBase, string>(n => n.Id, Is.Equal(id)));
            Assert.NotNull(savedNode);
            foreach (var prop in node.GetType().GetProperties())
            {
                if (!ObjectHelper.GetPrimitivity(prop).HasFlag(GraphPrimitivity.Object))
                {
                    Debug.WriteLine($"Checking {prop.Name} - {prop.GetValue(node)} == {prop.GetValue(savedNode)}");
                    Assert.Equal(prop.GetValue(node), prop.GetValue(savedNode));
                }
            }
            await DeleteTestData<NodeBase>(node.Notes);
            
        }

    }
}
