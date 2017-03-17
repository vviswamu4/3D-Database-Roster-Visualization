using UnityEngine;
using System.Collections;
using System;
using System.IO;
using System.Collections.Specialized;
using SimpleJSON;
using System.Net;
using System.Text;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Networking;
using UnityEngine.UI;
using UnityEngine.VR.WSA.Persistence;
using UnityEngine.VR.WSA;


public class GameController : MonoBehaviour
{
    public GameObject name;
    public GameObject persons;
    public GameObject[] personArray;
    GameObject[] personNameArray;
    public GameObject clusters;
    public GameObject[] clusterArray;
    GameObject[] clusterNameArray;
    private Vector3 screenPoint;
    private Vector3 offset;

    //class for Person Objects
    public class Person
    {
        public String name;
        public string type;
        public string prefab;
    }

    //class for Cluster Objects
    public class Cluster
    {
        public String name;
        public string prefab;
        public List<Person> mPerson = new List<Person>();
    }

    public class PersonCluster
    {
        public List<List<String>> person_cluster;
    }

    public class jsonValue
    {
        public List<string> columns;
        public List<List<string>> data;
    }

    /*class to make coroutine calls and return data from web requests*/
    public class CoroutineWithData
    {
        public Coroutine coroutine { get; private set; }
        public object result;
        private IEnumerator target;
        public CoroutineWithData(MonoBehaviour owner, IEnumerator target)
        {
            this.target = target;
            this.coroutine = owner.StartCoroutine(Run());
        }

        private IEnumerator Run()
        {
            while (target.MoveNext())
            {
                result = target.Current;
                yield return result;
            }
        }
    }



    class Program
    {
        Person ap1 = new Person();
        Dictionary<string, Person> personDict = new Dictionary<string, Person>();

        /*function returning a hashmap of <PersonName, PersonObject>*/

        public Dictionary<string, Person> getPerson(JSONNode n)
        {
            //var personDict = new Dictionary<string, Person>();
            var result = n["data"].AsArray;

            foreach (JSONNode rec in result)
            {
                //var record = rec.AsArray;
                var record = rec;
                if (!personDict.ContainsKey(record[0].ToString()))
                {
                    Person ap = new Person();
                    ap.name = record[0].ToString();
                    ap.type = record[1].ToString();
                    ap.prefab = record[2].ToString();
                    personDict.Add(ap.name, ap);
                }

            }
            return personDict;
        }


        /*function returning a hashmap of <ClusterName, ClusterObject>*/

        public Dictionary<string, Cluster> getCluster(JSONNode n)
        {
            var clusterDict = new Dictionary<string, Cluster>();
            var personDict = new Dictionary<string, Person>();
            var result = n["data"].AsArray;

            foreach (JSONNode rec in result)
            {
                var record = rec.AsArray;
                Cluster mc;
                if (!clusterDict.ContainsKey(record[0].ToString()))
                {
                    mc = new Cluster();
                    mc.name = record[0].ToString();
                    mc.prefab = record[4].ToString();
                    clusterDict.Add(mc.name, mc);
                }
                else
                {
                    mc = clusterDict[record[0].ToString()];
                }

                Person ap;
                if (!personDict.ContainsKey(record[1].ToString()))
                {
                    ap = new Person();
                    ap.name = record[1].ToString();
                    ap.type = record[2].ToString();
                    ap.prefab = record[3].ToString();
                    personDict.Add(ap.name, ap);
                }
                else
                {
                    ap = personDict[record[1].ToString()];
                }
                mc.mPerson.Add(ap);

            }

            return clusterDict;

        }
    }

    public Renderer rend;
    IEnumerator Start()
    {
        /*for debug*/
        //GameObject obj_new = (GameObject)Instantiate(Resources.Load("FreePack/Prefabs/Rocket"));
        //GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        //sphere.transform.position = new Vector3(0, 1F, 0);
        //obj_new.transform.position = new Vector3(1, 1, 1);

        //GameObject ground = GameObject.Find("Ground");
        //WorldAnchor anchor = ground.AddComponent<WorldAnchor>();
        //anchor.transform.position = new Vector3(-3, -2, 0);

        //GameObject sphere = GameObject.Find("Sphere");
        //WorldAnchor sanchor = sphere.AddComponent<WorldAnchor>();
        //sanchor.transform.position = new Vector3(0,1,-6);


        rend = GetComponent<Renderer>();

        /*coroutine call to get person objects*/
        CoroutineWithData cd = new CoroutineWithData(this, Upload("persons"));
        yield return cd.coroutine;
        JSONNode hp_persons_obj = (JSONNode)cd.result;

        /*coroutine call to get cluster objects*/
        CoroutineWithData cd1 = new CoroutineWithData(this, Upload("clusters"));
        yield return cd1.coroutine;
        JSONNode hp_clusters_obj = (JSONNode)cd1.result;

        /*coroutine call to get relationship between person and cluster objects*/
        CoroutineWithData cd2 = new CoroutineWithData(this, Upload("person-cluster"));
        yield return cd2.coroutine;
        JSONNode hp_persons_clusters_obj = (JSONNode)cd2.result;


        Program p = new Program();
        var personMap = new Dictionary<string, Person>();
        var clusterMap = new Dictionary<string, Cluster>();
        
        //for debug
        /*
        Person ap1 = new Person();
        ap1.name = "Vasudha Viswamurthy";
        ap1.type = "0";
        ap1.prefab = "Cowboy/Prefabs/Cowboy";
        personMap.Add(ap1.name, ap1);

        Cluster mc1 = new Cluster();
        mc1.name = "Mobile Systems Research Studio";
        mc1.prefab = "FreePack/Prefabs/Rocket";
        mc1.mPerson.Add(ap1);
        clusterMap.Add(mc1.name, mc1);
        */


        /* Get the hashmaps for person and cluster objects */
        personMap = p.getPerson(hp_persons_obj);
        clusterMap = p.getCluster(hp_persons_clusters_obj);
        
        /*Call to  Render GameObjects */
        //createSpheres(personMap, clusterMap);

    }




    void OnMouseEnter()
    {
        rend.material.color = Color.red;
    }
    void OnMouseOver()
    {
        rend.material.color -= new Color(0.1F, 0, 0) * Time.deltaTime;
    }
    void OnMouseExit()
    {
        rend.material.color = Color.white;
    }



    /*Function to make a web request and get data from Neo4j */
    IEnumerator Upload(string cdata)
    {
        string postData = "";
        if (cdata == "persons")
        {
            postData = "{ \"query\" : \"MATCH (n:Person) return n.name, n.type, n.prefab\"}";
        }
        else if (cdata == "clusters")
        {
            postData = "{ \"query\" : \"MATCH (m:Cluster) return m.name, m.prefab \"}";
        }
        else if (cdata == "person-cluster")
        {
            postData = "{ \"query\" : \"MATCH (n:Person)-[:BELONGS_TO]->(m:Cluster) return m.name, n.name, n.type, n.prefab, m.prefab \"}";
        }

        var request = new UnityWebRequest("http://192.168.0.112:7474/db/data/cypher", "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(postData);
        request.uploadHandler = (UploadHandler)new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.Send();

        /* FOR DEBUGGING */
        /*
        JSONNode j = null;

        if (cdata == "persons")
        {
            var s = "{\"columns\" : [ \"n.name\", \"n.type\", \"n.prefab\" ], \"data\" : [ [ \"Robert LiKamWa\", 0, \"Cowboy/Prefabs/Cowboy\" ], [ \"Vasudha Viswamurthy\", 2, \"UnityMaskMan/Prefabs/UnityMask\" ], [ \"Akshaya Ravishankar\", 2, \"UnityMaskMan/Prefabs/UnityMask\" ], [ \"Venkatesh Kodukula\", 2, \"UnityMaskMan/Prefabs/UnityMask\" ], [ \"Yitao Chen\", 1, \"BloodMageJonas/LOD1_unwrapped\" ], [ \"Saurabh Jagdhane\", 2, \"UnityMaskMan/Prefabs/UnityMask\" ], [ \"Pawan Turaga\", 0, \"Cowboy/Prefabs/Cowboy\" ], [ \"PhD Student1\", 1, \"BloodMageJonas/LOD1_unwrapped\" ], [ \"Research Student1\", 2, \"UnityMaskMan/Prefabs/UnityMask\" ], [ \"Research Student2\", 2, \"UnityMaskMan/Prefabs/UnityMask\" ], [ \"Research Student3\", 3, \"UnityMaskMan/Prefabs/UnityMask\" ], [ \"Staff1\", 4, \"Groucho/Prefab/Groucho\" ] ]}";
            j = JSON.Parse(s);
        }
        else if (cdata == "clusters")
        {
            var s = "{\"columns\" : [ \"m.name\", \"m.prefab\" ], \"data\" : [ [ \"Mobile Systems Research Studio\", \"FreePack/Prefabs/Rocket\" ], [ \"Image Processing Studio\", \"FreePack/Prefabs/Rocket\" ] ]}";
            j = JSON.Parse(s);
        }
        else if (cdata == "person-cluster")
        {
            var s = "{\"columns\" : [ \"m.name\", \"n.name\", \"n.type\", \"n.prefab\", \"m.prefab\" ], \"data\" : [ [ \"Mobile Systems Research Studio\", \"Vasudha V\", 3, \"UnityMaskMan/Prefabs/UnityMask\", \"FreePack/Prefabs/Rocket\" ], [ \"Mobile Systems Research Studio\", \"Saurabh Jagdhane\", 2, \"UnityMaskMan/Prefabs/UnityMask\", \"FreePack/Prefabs/Rocket\" ], [ \"Mobile Systems Research Studio\", \"Yitao Chen\", 1, \"BloodMageJonas/LOD1_unwrapped\", \"FreePack/Prefabs/Rocket\" ], [ \"Mobile Systems Research Studio\", \"Venkatesh Kodukula\", 2, \"UnityMaskMan/Prefabs/UnityMask\", \"FreePack/Prefabs/Rocket\" ], [ \"Mobile Systems Research Studio\", \"Akshaya Ravishankar\", 2, \"UnityMaskMan/Prefabs/UnityMask\", \"FreePack/Prefabs/Rocket\" ], [ \"Mobile Systems Research Studio\", \"Vasudha Viswamurthy\", 2, \"UnityMaskMan/Prefabs/UnityMask\", \"FreePack/Prefabs/Rocket\" ], [ \"Mobile Systems Research Studio\", \"Robert LiKamWa\", 0, \"Cowboy/Prefabs/Cowboy\", \"FreePack/Prefabs/Rocket\" ], [ \"Image Processing Studio\", \"Staff1\", 4, \"Groucho/Prefab/Groucho\", \"FreePack/Prefabs/Rocket\" ], [ \"Image Processing Studio\", \"Research Student3\", 3, \"UnityMaskMan/Prefabs/UnityMask\", \"FreePack/Prefabs/Rocket\" ], [ \"Image Processing Studio\", \"Research Student2\", 2, \"UnityMaskMan/Prefabs/UnityMask\", \"FreePack/Prefabs/Rocket\" ], [ \"Image Processing Studio\", \"Research Student1\", 2, \"UnityMaskMan/Prefabs/UnityMask\", \"FreePack/Prefabs/Rocket\" ], [ \"Image Processing Studio\", \"PhD Student1\", 1, \"BloodMageJonas/LOD1_unwrapped\", \"FreePack/Prefabs/Rocket\" ], [ \"Image Processing Studio\", \"Pawan Turaga\", 0, \"Cowboy/Prefabs/Cowboy\", \"FreePack/Prefabs/Rocket\" ] ]}";
            j = JSON.Parse(s);
        }

        */
        JSONNode j = JSON.Parse(request.downloadHandler.text);
        print(request.downloadHandler.text);

        yield return j;

    }

    /*function to position Person objects in a circle around cluster objects*/
    public Vector3 RandomCircle(Vector3 center, float radius, float a)
    {
        float ang = a;
        Vector3 pos;
        pos.x = center.x + radius * Mathf.Sin(ang * Mathf.Deg2Rad);
        pos.z = center.z + radius * Mathf.Cos(ang * Mathf.Deg2Rad);
        pos.y = center.y - 1;
        return pos;
    }

   /* void OnMouseDown()
    {
        screenPoint = Camera.main.WorldToScreenPoint(transform.position);
        offset = transform.position - Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, screenPoint.z));
    }

    void OnMouseDrag()
    {
        Vector3 curScreenPoint = new Vector3(Input.mousePosition.x, Input.mousePosition.y, screenPoint.z);
        Vector3 curPosition = Camera.main.ScreenToWorldPoint(curScreenPoint) + offset;
        transform.position = curPosition;
    }

    */

    /*Function to render gamobjects*/
    void createSpheres(Dictionary<string, Person> personMap, Dictionary<string, Cluster> clusterMap)
    {
        List<string> person_list = personMap.Keys.ToList<string>();
        List<string> cluster_list = clusterMap.Keys.ToList<string>();
                    
        personArray = new GameObject[person_list.Count];
        personNameArray = new GameObject[person_list.Count];
        clusterArray = new GameObject[cluster_list.Count];
        clusterNameArray = new GameObject[cluster_list.Count];
        

        Vector3 initialPersonPosition = new Vector3(-7, 0, 0);
        Vector3 tempPersonNamePosition = initialPersonPosition + new Vector3(0, 1, 0);
        Vector3 initialClusterPosition = new Vector3(-5, 0, 0);
        Vector3 tempClusterNamePosition = initialClusterPosition + new Vector3(0, 1, 0);

        int k = 0;
        float x = -5;
        float y = 0;
        float z = 0;
      

        for (int i = 0; i < cluster_list.Count; i++)
        {
            string cluster_path = clusterMap[cluster_list[i]].prefab;
            
            var cluster_str_old = clusterMap[cluster_list[i]].prefab.ToString();
            var cluster_str = cluster_str_old.Replace(@"""", "");
            clusterArray[i] = (GameObject)Instantiate(Resources.Load(cluster_str));

            GameObject name = new GameObject();
            clusterNameArray[i] = (GameObject)Instantiate(name, tempClusterNamePosition, Quaternion.identity);
            clusterNameArray[i].AddComponent(typeof(TextMesh));
            TextMesh tempText = clusterNameArray[i].GetComponent<TextMesh>();
            tempText.text = cluster_list[i];

            Vector3 newMoviePosition = new Vector3(x + k, y, z);
            if (k == 0)
            {
                k = 5;

            }
            else
            {
                k = 0;
                z = z + 5;

            }
            clusterArray[i].transform.position = newMoviePosition;
            clusterNameArray[i].transform.position = newMoviePosition + new Vector3(-1.5f, 9, 0);
            clusterArray[i].transform.localScale += new Vector3(2f, 2f, 2f);//5f,5f,5f
            clusterNameArray[i].transform.localScale -= new Vector3(0.59f, 0.59f, 0.59f);
            clusterArray[i].name = cluster_list[i];

            int person_count = clusterMap[cluster_list[i]].mPerson.Count;
            personArray = new GameObject[person_count];

            for (int j = 0; j < person_count; j++)
            {

                string path = clusterMap[cluster_list[i]].mPerson[j].prefab;
                var str_old = clusterMap[cluster_list[i]].mPerson[j].prefab.ToString();
                var str = str_old.Replace(@"""", "");
                personArray[j] = (GameObject)Instantiate(Resources.Load(str));
                

                personNameArray[j] = (GameObject)Instantiate(name, tempPersonNamePosition, Quaternion.identity);
                personNameArray[j].AddComponent(typeof(TextMesh));
                TextMesh tempNameText = personNameArray[j].GetComponent<TextMesh>();
                tempNameText.text = clusterMap[cluster_list[i]].mPerson[j].name;



                var ran_angle = 360 / person_count;
                var a = j * ran_angle;
                Vector3 newPersonPosition = RandomCircle(newMoviePosition, 1.0f, a);
                personArray[j].transform.position = newPersonPosition;
                personNameArray[j].transform.position = newPersonPosition + new Vector3(0, 2.5f + j, 0);
                personArray[j].transform.localRotation = new Quaternion(0, 180, 0, 0);
                personArray[j].transform.localScale += new Vector3(0.001f, 0.001f, 0.001f);//0.01f

                personNameArray[j].transform.localScale -= new Vector3(0.59f, 0.59f, 0.59f);
                personNameArray[j].name = clusterMap[cluster_list[i]].mPerson[j].name + "_name";
                personArray[j].name = clusterMap[cluster_list[i]].mPerson[j].name;
                var boxCollider1 = (BoxCollider)personArray[j].AddComponent<BoxCollider>();
                boxCollider1.isTrigger = true;
                Physics.queriesHitTriggers = true;

                var go = new GameObject();
                var lr = go.AddComponent<LineRenderer>();

                var person_object = GameObject.Find(clusterMap[cluster_list[i]].mPerson[j].name);
                var cluster_object = GameObject.Find(cluster_list[i]);
                lr.SetPosition(0, person_object.transform.position);
                lr.SetPosition(1, cluster_object.transform.position);
                lr.SetColors(Color.red, Color.green);
                lr.SetWidth(0.1f,0.1f);
                //lr.material = new Material(Shader.Find("Particles/Additive"));

                var go1 = new GameObject();
                var lr1 = go1.AddComponent<LineRenderer>();
              
                var person_name_object = GameObject.Find(clusterMap[cluster_list[i]].mPerson[j].name + "_name");
                lr1.SetPosition(0, person_object.transform.position);
                lr1.SetPosition(1, person_name_object.transform.position);
                
                lr1.SetColors(Color.white, Color.white);
                lr1.SetWidth(0.04f, 0.04f);
                //lr1.material = new Material(Shader.Find("Particles/Additive"));

           }
        }
    }



    void OnMouseDown()
    {
        screenPoint = Camera.main.WorldToScreenPoint(transform.position);
        offset = transform.position - Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, screenPoint.z));
    }

    void OnMouseDrag()
    {
        Vector3 curScreenPoint = new Vector3(Input.mousePosition.x, Input.mousePosition.y, screenPoint.z);
        Vector3 curPosition = Camera.main.ScreenToWorldPoint(curScreenPoint) + offset;
        transform.position = curPosition;
    }
   

}










