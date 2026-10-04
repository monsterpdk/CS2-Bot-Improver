using NadeSystem;
static GrenadeData Nade() => new() {
    ProjectilePosition = new Vec3 { X=16, Z=64 },
    ProjectileVelocity = new Vec3 { X=700, Z=300 },
    LandingPosition = new Vec3 { X=800 }
};
var eye = new Vec3 { Z=64 };
void Check(string name, GrenadeData grenade, string? expected, float yaw=0, float pitch=0, Vec3? position=null) {
    var actual = ReplaySafety.Reject(position ?? eye, yaw, pitch, grenade);
    if (actual != expected) throw new Exception($"{name}: expected {expected ?? "accepted"}, got {actual ?? "accepted"}");
    Console.WriteLine("PASS " + name);
}
Check("ordinary forward throw", Nade(), null);
Check("distant bot cannot borrow a release", Nade(), "remoteRelease", position:new Vec3 { X=500, Z=64 });
var grenade=Nade(); grenade.ProjectileVelocity.X=-700;
Check("backward explosive rejected", grenade, "wrongHeading");
grenade=Nade(); grenade.ProjectileVelocity.X=0; grenade.ProjectileVelocity.Y=700;
Check("sideways explosive rejected", grenade, "wrongHeading");
Check("turned bot permits same direction", grenade, null, yaw:90);
grenade=Nade(); grenade.ProjectileVelocity.X=1001; grenade.ProjectileVelocity.Z=0;
Check("excess speed rejected", grenade, "excessSpeed");
grenade=Nade(); grenade.LandingPosition.X=1817;
Check("excess distance rejected", grenade, "excessRange");
grenade=Nade(); grenade.ProjectilePosition.X=float.NaN;
Check("nonfinite position rejected", grenade, "invalidGeometry");
Check("upward lob while looking down rejected", Nade(), "wrongPitch", pitch:80);
grenade=Nade(); grenade.ProjectilePosition.X=64;
Check("release boundary accepted", grenade, null);
grenade.ProjectilePosition.X=64.1f;
Check("release boundary exceeded", grenade, "remoteRelease");
